import axios from "axios";
import Database from "better-sqlite3";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const db = new Database(path.join(__dirname, "..", "futbol.db"));

db.exec(`
CREATE TABLE IF NOT EXISTS Matches (
Id INTEGER PRIMARY KEY,
Season TEXT,
League TEXT,
Week INTEGER,
Date TEXT,
Time TEXT,
HomeTeam TEXT,
AwayTeam TEXT,
Winner TEXT,
GoalHome INTEGER,
GoalAway INTEGER,
CornerHome INTEGER,
CornerAway INTEGER,
YellowHome INTEGER,
YellowAway INTEGER,
RedHome INTEGER,
RedAway INTEGER,
ShotsHome INTEGER,
ShotsAway INTEGER,
ShotsOnTargetHome INTEGER,
ShotsOnTargetAway INTEGER,
FoulsHome INTEGER,
FoulsAway INTEGER,
PossessionHome INTEGER,
PossessionAway INTEGER,
HomeGoalsMinutes TEXT,
AwayGoalsMinutes TEXT
)
`);

db.exec(`
CREATE UNIQUE INDEX IF NOT EXISTS idx_matches_natural_key
ON Matches (Season, League, HomeTeam, AwayTeam, Date)
`);

const row = db.prepare(`SELECT COUNT(*) as count FROM Matches`).get();
const hasData = row.count > 0;

function addDays(date, days) {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}

const today = new Date();
today.setHours(0, 0, 0, 0);

const seasonStart = hasData
  ? addDays(today, -7)
  : new Date("2025-08-08");

// Incremental modda sadece son hafta + yakın gelecek; ilk yüklemede tüm sezon
const seasonEnd = hasData
  ? addDays(today, 14)
  : new Date("2026-09-01");

const insertMatch = db.prepare(`
INSERT OR REPLACE INTO Matches VALUES (
@Id,@Season,@League,@Week,@Date,@Time,
@HomeTeam,@AwayTeam,@Winner,
@GoalHome,@GoalAway,
@CornerHome,@CornerAway,
@YellowHome,@YellowAway,
@RedHome,@RedAway,
@ShotsHome,@ShotsAway,
@ShotsOnTargetHome,@ShotsOnTargetAway,
@FoulsHome,@FoulsAway,
@PossessionHome,@PossessionAway,
@HomeGoalsMinutes,@AwayGoalsMinutes
)
`);

const findByNaturalKey = db.prepare(`
SELECT Id FROM Matches
WHERE Season = @Season
  AND League = @League
  AND HomeTeam = @HomeTeam
  AND AwayTeam = @AwayTeam
  AND Date = @Date
  AND Id != @Id
`);

const findStaleTbdFixtures = db.prepare(`
SELECT Id FROM Matches
WHERE Season = @Season
  AND League = @League
  AND HomeTeam = @HomeTeam
  AND AwayTeam = @AwayTeam
  AND Id != @Id
  AND Date != @Date
  AND Winner = 'TBD'
`);

const deleteById = db.prepare(`DELETE FROM Matches WHERE Id = ?`);

function upsertMatch(match) {
  for (const stale of findStaleTbdFixtures.all(match)) {
    deleteById.run(stale.Id);
  }

  const sameFixture = findByNaturalKey.get(match);
  if (sameFixture) {
    deleteById.run(sameFixture.Id);
  }

  insertMatch.run(match);
}

function cleanupExistingDuplicates() {
  const removedNatural = db.prepare(`
    DELETE FROM Matches
    WHERE Id IN (
      SELECT m1.Id
      FROM Matches m1
      JOIN Matches m2
        ON m1.Season = m2.Season
       AND m1.League = m2.League
       AND m1.HomeTeam = m2.HomeTeam
       AND m1.AwayTeam = m2.AwayTeam
       AND m1.Date = m2.Date
       AND m1.Id < m2.Id
    )
  `).run().changes;

  const removedStale = db.prepare(`
    DELETE FROM Matches
    WHERE Id IN (
      SELECT m1.Id
      FROM Matches m1
      JOIN Matches m2
        ON m1.Season = m2.Season
       AND m1.League = m2.League
       AND m1.HomeTeam = m2.HomeTeam
       AND m1.AwayTeam = m2.AwayTeam
       AND m1.Id != m2.Id
       AND m1.Date != m2.Date
       AND m1.Winner = 'TBD'
       AND m2.Winner != 'TBD'
    )
  `).run().changes;

  if (removedNatural + removedStale > 0) {
    console.log(`🧹 ${removedNatural + removedStale} duplicate kayıt temizlendi`);
  }
}

const leagues = [
  { code: "eng.1", name: "Premier League" },
  { code: "eng.2", name: "EFL Championship" },
  { code: "tur.1", name: "Super Lig" },
  { code: "esp.1", name: "LaLiga" },
  { code: "ita.1", name: "Serie A" },
  { code: "ger.1", name: "Bundesliga" },
  { code: "fra.1", name: "Ligue 1" },
  { code: "ned.1", name: "Eredivisie" },
  { code: "por.1", name: "Primeira Liga" },
  { code: "bel.1", name: "Pro League" },
  { code: "uefa.champions", name: "UEFA Champions League" },
  { code: "uefa.europa", name: "UEFA Europa League" },
  { code: "uefa.europa.conf", name: "UEFA Europa Conference League" },
  { code: "ksa.1", name: "Saudi Pro League" },
  { code: "fifa.world", name: "FIFA World Cup" }
];

function formatDate(date) {
  return date.toISOString().slice(0, 10).replace(/-/g, "");
}

function getWeekNumber(date) {
  const base = new Date("2025-08-08");
  const diff = Math.floor((date - base) / (1000 * 60 * 60 * 24));
  return diff >= 0 ? Math.floor(diff / 7) + 1 : 0;
}

async function fetchScoreboard(leagueCode, start, end) {
  const url = `https://site.api.espn.com/apis/site/v2/sports/soccer/${leagueCode}/scoreboard?dates=${formatDate(start)}-${formatDate(end)}`;
  const { data } = await axios.get(url);
  return data.events || [];
}

function parseMatch(event, leagueName) {
  const competition = event.competitions?.[0];
  if (!competition) return null;

  const home = competition.competitors?.find(c => c.homeAway === "home");
  const away = competition.competitors?.find(c => c.homeAway === "away");
  if (!home || !away) return null;

  const [datePart, timePartFull] = event.date.split("T");
  const timePart = timePartFull?.slice(0, 5);

  const isPlayed = competition.status?.type?.state === "post";

  let yellowHome = 0, yellowAway = 0, redHome = 0, redAway = 0;
  let cornerHome = 0, cornerAway = 0;
  let shotsHome = 0, shotsAway = 0;
  let shotsOnTargetHome = 0, shotsOnTargetAway = 0;
  let foulsHome = 0, foulsAway = 0;
  let possessionHome = 0, possessionAway = 0;
  let homeGoalsMinutes = [], awayGoalsMinutes = [];

  if (isPlayed) {
    const stat = (team, name) =>
      Number(team.statistics?.find(s => s.name === name)?.displayValue || 0);

    cornerHome = stat(home, "wonCorners");
    cornerAway = stat(away, "wonCorners");

    shotsHome = stat(home, "totalShots");
    shotsAway = stat(away, "totalShots");

    shotsOnTargetHome = stat(home, "shotsOnTarget");
    shotsOnTargetAway = stat(away, "shotsOnTarget");

    foulsHome = stat(home, "foulsCommitted");
    foulsAway = stat(away, "foulsCommitted");

    possessionHome = Number(
      home.statistics?.find(s => s.name === "possessionPct")?.displayValue?.replace("%", "") || 0
    );
    possessionAway = Number(
      away.statistics?.find(s => s.name === "possessionPct")?.displayValue?.replace("%", "") || 0
    );

    if (Array.isArray(competition.details)) {
      for (const d of competition.details) {
        if (d.yellowCard) {
          d.team?.id === home.team.id ? yellowHome++ : yellowAway++;
        }
        if (d.redCard) {
          d.team?.id === home.team.id ? redHome++ : redAway++;
        }

        if (d.type?.text === "Goal" && d.clock?.displayValue) {
          const minute = d.clock.displayValue.replace("'", "");
          d.team?.id === home.team.id
            ? homeGoalsMinutes.push(minute)
            : awayGoalsMinutes.push(minute);
        }
      }
    }
  }

  return {
    Id: Number(event.id),
    Season: "2025-2026",
    League: leagueName,
    Week: getWeekNumber(new Date(event.date)),
    Date: datePart,
    Time: timePart,

    HomeTeam: home.team.displayName,
    AwayTeam: away.team.displayName,

    Winner: isPlayed
      ? home.score === away.score
        ? "Draw"
        : Number(home.score) > Number(away.score)
        ? "Home"
        : "Away"
      : "TBD",

    GoalHome: isPlayed ? Number(home.score) : 0,
    GoalAway: isPlayed ? Number(away.score) : 0,

    CornerHome: cornerHome,
    CornerAway: cornerAway,

    YellowHome: yellowHome,
    YellowAway: yellowAway,
    RedHome: redHome,
    RedAway: redAway,

    ShotsHome: shotsHome,
    ShotsAway: shotsAway,
    ShotsOnTargetHome: shotsOnTargetHome,
    ShotsOnTargetAway: shotsOnTargetAway,

    FoulsHome: foulsHome,
    FoulsAway: foulsAway,

    PossessionHome: possessionHome,
    PossessionAway: possessionAway,

    HomeGoalsMinutes: homeGoalsMinutes.join("|"),
    AwayGoalsMinutes: awayGoalsMinutes.join("|")
  };
}

async function run() {
  cleanupExistingDuplicates();

  const matchById = new Map();

  for (const league of leagues) {
    console.log(`⏳ ${league.name}`);

    let cursor = new Date(seasonStart);

    while (cursor <= seasonEnd) {
      const rangeEnd = addDays(cursor, 6);

      const events = await fetchScoreboard(
        league.code,
        cursor,
        rangeEnd > seasonEnd ? seasonEnd : rangeEnd
      );

      for (const event of events) {
        const match = parseMatch(event, league.name);
        if (match) matchById.set(match.Id, match);
      }

      cursor = addDays(rangeEnd, 1);
      await new Promise(r => setTimeout(r, 250));
    }
  }

  const upsertMany = db.transaction((matches) => {
    for (const m of matches) upsertMatch(m);
  });

  upsertMany([...matchById.values()]);

  console.log(`✅ ${matchById.size} maç işlendi (${hasData ? "incremental" : "full"} mod)`);
  db.close();
}

run();
