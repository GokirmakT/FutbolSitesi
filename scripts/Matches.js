import axios from "axios";
import Database from "better-sqlite3";
import { fileURLToPath } from "url";
import { dirname, join } from "path";

/* ----------------- SQLITE ----------------- */
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const db = new Database(join(__dirname, "..", "futbol.db"));

// Tabloyu sıfırla
db.exec(`
DROP TABLE IF EXISTS Matches;

CREATE TABLE Matches (
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

const insertMatch = db.prepare(`
INSERT INTO Matches VALUES (
@MatchId,@Season,@League,@Week,@Date,@Time,
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

/* ----------------- LIG LISTESI ----------------- */
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
  { code: "ksa.1", name: "Saudi Pro League" }
];

/* ----------------- UTILS ----------------- */
function formatDate(date) {
  return date.toISOString().slice(0, 10).replace(/-/g, "");
}

function addDays(date, days) {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}

function getWeekNumber(date) {
  const seasonStart = new Date("2025-08-08");
  const diff = Math.floor((date - seasonStart) / (1000 * 60 * 60 * 24));
  return diff >= 0 ? Math.floor(diff / 7) + 1 : 0;
}

/* ----------------- FETCH ----------------- */
async function fetchScoreboard(leagueCode, start, end) {
  const url = `https://site.api.espn.com/apis/site/v2/sports/soccer/${leagueCode}/scoreboard?dates=${formatDate(
    start
  )}-${formatDate(end)}`;
  const { data } = await axios.get(url);
  return data.events || [];
}

/* ----------------- PARSE ----------------- */
function parseMatch(event, leagueName, matchId) {
  const competition = event.competitions?.[0];
  if (!competition) return null;

  const home = competition.competitors?.find(c => c.homeAway === "home");
  const away = competition.competitors?.find(c => c.homeAway === "away");
  if (!home || !away) return null;

  const matchDate = new Date(event.date);
  const [datePart, timePartFull] = event.date.split("T");
  const timePart = timePartFull?.slice(0, 5);
  const state = competition.status?.type?.state;
  const isPlayed = state === "post";

  let yellowHome = 0, yellowAway = 0, redHome = 0, redAway = 0;
  let cornerHome = 0, cornerAway = 0;
  let shotsHome = 0, shotsAway = 0;
  let shotsOnTargetHome = 0, shotsOnTargetAway = 0;
  let foulsHome = 0, foulsAway = 0;
  let possessionHome = 0, possessionAway = 0;
  let homeGoalsMinutes = [], awayGoalsMinutes = [];

  if (isPlayed) {
    const stat = (team, name) => Number(team.statistics?.find(s => s.name === name)?.displayValue || 0);

    cornerHome = stat(home, "wonCorners");
    cornerAway = stat(away, "wonCorners");

    shotsHome = stat(home, "totalShots");
    shotsAway = stat(away, "totalShots");

    shotsOnTargetHome = stat(home, "shotsOnTarget");
    shotsOnTargetAway = stat(away, "shotsOnTarget");

    foulsHome = stat(home, "foulsCommitted");
    foulsAway = stat(away, "foulsCommitted");

    possessionHome = Number(home.statistics?.find(s => s.name === "possessionPct")?.displayValue?.replace("%","") || 0);
    possessionAway = Number(away.statistics?.find(s => s.name === "possessionPct")?.displayValue?.replace("%","") || 0);

    if (Array.isArray(competition.details)) {
      for (const d of competition.details) {
        if (d.yellowCard) d.team?.id === home.team.id ? yellowHome++ : yellowAway++;
        if (d.redCard) d.team?.id === home.team.id ? redHome++ : redAway++;

        if (d.type?.text === "Goal" && d.clock?.displayValue) {
          const minute = d.clock.displayValue.replace("'", "");
          d.team?.id === home.team.id ? homeGoalsMinutes.push(minute) : awayGoalsMinutes.push(minute);
        }
      }
    }
  }

  return {
    MatchId: matchId,
    Season: "2025-2026",
    League: leagueName,
    Week: getWeekNumber(matchDate),
    Date: datePart,
    Time: timePart,
    HomeTeam: home.team.displayName,
    AwayTeam: away.team.displayName,
    Winner: isPlayed ? (home.score === away.score ? "Draw" : (Number(home.score) > Number(away.score) ? "Home" : "Away")) : "TBD",
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

/* ----------------- RUN ----------------- */
async function run() {
  const seasonStart = new Date("2025-08-08");
  const seasonEnd = new Date("2026-06-01");
  let allMatches = [];
  let matchId = 1;

  for (const league of leagues) {
    console.log(`⏳ ${league.name} fikstürü çekiliyor...`);
    let cursor = seasonStart;

    while (cursor <= seasonEnd) {
      const rangeEnd = addDays(cursor, 6);
      const events = await fetchScoreboard(league.code, cursor, rangeEnd);

      for (const event of events) {
        const match = parseMatch(event, league.name, matchId++);
        if (match) allMatches.push(match);
      }

      cursor = addDays(rangeEnd, 1);
      await new Promise(r => setTimeout(r, 400));
    }
  }

  for (const m of allMatches) {
    insertMatch.run(m);
  }

  console.log(`✅ TOPLAM ${allMatches.length} maç DB'ye yazıldı`);
  db.close();
}

run();