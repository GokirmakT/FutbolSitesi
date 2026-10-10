using Microsoft.AspNetCore.Mvc;
using FutbolSitesi.Models;
using FutbolSitesi.Data;
using FutbolSitesi.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace FutbolSitesi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MatchesController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/matches/options
        [HttpGet("options")]
        public async Task<IActionResult> GetMatchOptions()
        {
            var options = await _db.Matches
                .AsNoTracking()
                .Select(match => new { match.Season, match.League })
                .Distinct()
                .OrderBy(match => match.Season)
                .ThenBy(match => match.League)
                .ToListAsync();
            var defaultSeason = await _db.Matches
                .AsNoTracking()
                .Where(match => match.Winner != "TBD")
                .OrderByDescending(match => match.Season)
                .Select(match => match.Season)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                seasons = options.Select(option => option.Season).Distinct().ToList(),
                defaultSeason,
                leaguesBySeason = options
                    .GroupBy(option => option.Season)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(option => option.League).Distinct().ToList())
            });
        }

        // GET /api/matches/fixtures?season=...&league=...&date=...&team=...
        [HttpGet("fixtures")]
        public async Task<IActionResult> GetFixtures(
            [FromQuery] string? season = null,
            [FromQuery] string? league = null,
            [FromQuery] DateTime? date = null,
            [FromQuery] string? team = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(season) &&
                string.IsNullOrWhiteSpace(league) &&
                date == null &&
                string.IsNullOrWhiteSpace(team))
            {
                return BadRequest(new
                {
                    message = "En az bir filtre (season, league, date veya team) zorunludur."
                });
            }

            if (page < 1 || pageSize < 1)
            {
                return BadRequest(new { message = "page ve pageSize pozitif olmalıdır." });
            }

            pageSize = Math.Min(pageSize, 100);

            var query = _db.Matches.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(season))
                query = query.Where(match => match.Season == season);
            if (!string.IsNullOrWhiteSpace(league))
                query = query.Where(match => match.League == league);
            if (date.HasValue)
            {
                var matchDate = date.Value.Date;
                query = query.Where(match => match.Date.Date == matchDate);
            }
            if (!string.IsNullOrWhiteSpace(team))
                query = query.Where(match => match.HomeTeam == team || match.AwayTeam == team);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(match => match.Date)
                .ThenBy(match => match.Time)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(match => new MatchFixtureDto(
                    match.Id,
                    match.Season,
                    match.League,
                    match.Week,
                    match.Date,
                    match.Time,
                    match.HomeTeam,
                    match.AwayTeam,
                    match.Winner,
                    match.GoalHome,
                    match.GoalAway))
                .ToListAsync();

            return Ok(new { page, pageSize, totalCount, items });
        }

        // GET /api/matches/analysis?season=...&league=...&team=...
        [HttpGet("analysis")]
        public async Task<IActionResult> GetAnalysisMatches(
            [FromQuery] string? season = null,
            [FromQuery] string? league = null,
            [FromQuery] string? team = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(season))
                return BadRequest(new { message = "season zorunludur." });

            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "page ve pageSize pozitif olmalıdır." });

            pageSize = Math.Min(pageSize, 100);

            var query = _db.Matches
                .AsNoTracking()
                .Where(match => match.Season == season);

            if (!string.IsNullOrWhiteSpace(league))
                query = query.Where(match => match.League == league);
            if (!string.IsNullOrWhiteSpace(team))
                query = query.Where(match => match.HomeTeam == team || match.AwayTeam == team);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(match => match.Date)
                .ThenBy(match => match.Time)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { page, pageSize, totalCount, items });
        }

        // GET /api/matches/analysis/by-date?season=...&date=yyyy-MM-dd
        [HttpGet("analysis/by-date")]
        public async Task<IActionResult> GetDailyAnalysisMatches(
            [FromQuery] string? season,
            [FromQuery] DateOnly? date,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(season))
                return BadRequest(new { message = "season zorunludur." });
            if (!date.HasValue)
                return BadRequest(new { message = "date zorunludur (yyyy-MM-dd)." });

            var dayStart = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var candidateMatches = await _db.Matches
                .AsNoTracking()
                .Where(match =>
                    match.Season == season &&
                    match.Date >= dayStart.AddDays(-1) &&
                    match.Date < dayStart.AddDays(1))
                .ToListAsync(cancellationToken);

            var items = candidateMatches
                .Where(match => GetTurkeyDate(match) == date.Value)
                .OrderBy(match => match.Date)
                .ThenBy(match => match.Time)
                .ToList();

            var leagues = await _db.Matches
                .AsNoTracking()
                .Where(match => match.Season == season)
                .Select(match => match.League)
                .Distinct()
                .OrderBy(league => league)
                .ToListAsync(cancellationToken);

            if (items.Count == 0)
            {
                return Ok(new
                {
                    date = date.Value,
                    season,
                    leagues,
                    items,
                    goalStatsByLeague = new Dictionary<string, object>(),
                    cornerStatsByLeague = new Dictionary<string, object>(),
                    cardStatsByLeague = new Dictionary<string, object>()
                });
            }

            var teamNames = items
                .SelectMany(match => new[] { match.HomeTeam, match.AwayTeam })
                .Distinct()
                .ToArray();
            var targetTeams = teamNames.ToHashSet(StringComparer.Ordinal);
            var leagueNames = items.Select(match => match.League).Distinct().ToArray();
            var historyCutoff = dayStart.AddDays(1);

            var history = await _db.Matches
                .AsNoTracking()
                .Where(match =>
                    match.Season == season &&
                    match.Winner != "TBD" &&
                    match.Date < historyCutoff &&
                    leagueNames.Contains(match.League) &&
                    (teamNames.Contains(match.HomeTeam) || teamNames.Contains(match.AwayTeam)))
                .Select(match => new MatchStatsSource(
                    match.Date,
                    match.Time,
                    match.League,
                    match.HomeTeam,
                    match.AwayTeam,
                    match.GoalHome,
                    match.GoalAway,
                    match.CornerHome,
                    match.CornerAway,
                    match.YellowHome,
                    match.YellowAway,
                    match.RedHome,
                    match.RedAway))
                .ToListAsync(cancellationToken);

            var teamStats = new Dictionary<string, Dictionary<string, TeamStatsAccumulator>>();
            foreach (var match in history.Where(match =>
                         GetTurkeyDate(match.Date, match.Time) < date.Value))
            {
                if (targetTeams.Contains(match.HomeTeam))
                    AddTeamStats(teamStats, match.League, match.HomeTeam, match);
                if (targetTeams.Contains(match.AwayTeam))
                    AddTeamStats(teamStats, match.League, match.AwayTeam, match);
            }

            var goalStatsByLeague = teamStats.ToDictionary(
                league => league.Key,
                league => league.Value.Select(team => new
                {
                    team = team.Key,
                    over15Rate = Rate(team.Value.GoalOver15Count, team.Value.MatchCount),
                    over25Rate = Rate(team.Value.GoalOver25Count, team.Value.MatchCount),
                    over35Rate = Rate(team.Value.GoalOver35Count, team.Value.MatchCount)
                }).ToList());
            var cornerStatsByLeague = teamStats.ToDictionary(
                league => league.Key,
                league => league.Value.Select(team => new
                {
                    team = team.Key,
                    over85Rate = Rate(team.Value.CornerOver85Count, team.Value.MatchCount),
                    over95Rate = Rate(team.Value.CornerOver95Count, team.Value.MatchCount),
                    over105Rate = Rate(team.Value.CornerOver105Count, team.Value.MatchCount)
                }).ToList());
            var cardStatsByLeague = teamStats.ToDictionary(
                league => league.Key,
                league => league.Value.Select(team => new
                {
                    team = team.Key,
                    penaltyOver25Rate = Rate(team.Value.PenaltyOver25Count, team.Value.MatchCount),
                    penaltyOver35Rate = Rate(team.Value.PenaltyOver35Count, team.Value.MatchCount),
                    penaltyOver45Rate = Rate(team.Value.PenaltyOver45Count, team.Value.MatchCount)
                }).ToList());

            return Ok(new
            {
                date = date.Value,
                season,
                leagues,
                items,
                goalStatsByLeague,
                cornerStatsByLeague,
                cardStatsByLeague
            });
        }

        // GET /api/matches/team/{team}?season=...&league=...&page=1&pageSize=50
        [HttpGet("team/{team}")]
        public async Task<IActionResult> GetTeamMatches(
            string team,
            [FromQuery] string? season = null,
            [FromQuery] string? league = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(team))
                return BadRequest(new { message = "team zorunludur." });

            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "page ve pageSize pozitif olmalıdır." });

            pageSize = Math.Min(pageSize, 100);

            var query = _db.Matches
                .AsNoTracking()
                .Where(match => match.HomeTeam == team || match.AwayTeam == team);

            if (!string.IsNullOrWhiteSpace(season))
                query = query.Where(match => match.Season == season);
            if (!string.IsNullOrWhiteSpace(league))
                query = query.Where(match => match.League == league);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(match => match.Date)
                .ThenByDescending(match => match.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { page, pageSize, totalCount, items });
        }

        // GET /api/matches/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMatchById(int id)
        {
            var match = await _db.Matches
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (match == null) return NotFound();
            return Ok(match);
        }

        private static DateOnly GetTurkeyDate(Match match) =>
            GetTurkeyDate(match.Date, match.Time);

        private static DateOnly GetTurkeyDate(DateTime utcDate, string utcTime)
        {
            var date = DateOnly.FromDateTime(utcDate);
            if (TimeSpan.TryParse(utcTime, out var time) && time >= TimeSpan.FromHours(21))
                date = date.AddDays(1);
            return date;
        }

        private static void AddTeamStats(
            IDictionary<string, Dictionary<string, TeamStatsAccumulator>> stats,
            string league,
            string team,
            MatchStatsSource match)
        {
            if (!stats.TryGetValue(league, out var leagueStats))
            {
                leagueStats = new Dictionary<string, TeamStatsAccumulator>();
                stats[league] = leagueStats;
            }

            if (!leagueStats.TryGetValue(team, out var teamStats))
            {
                teamStats = new TeamStatsAccumulator();
                leagueStats[team] = teamStats;
            }

            teamStats.MatchCount++;
            var totalGoals = match.GoalHome + match.GoalAway;
            if (totalGoals > 1.5) teamStats.GoalOver15Count++;
            if (totalGoals > 2.5) teamStats.GoalOver25Count++;
            if (totalGoals > 3.5) teamStats.GoalOver35Count++;

            var totalCorners = match.CornerHome + match.CornerAway;
            if (totalCorners > 8.5) teamStats.CornerOver85Count++;
            if (totalCorners > 9.5) teamStats.CornerOver95Count++;
            if (totalCorners > 10.5) teamStats.CornerOver105Count++;

            var penaltyScore =
                match.YellowHome + match.YellowAway +
                (match.RedHome + match.RedAway) * 2;
            if (penaltyScore > 2.5) teamStats.PenaltyOver25Count++;
            if (penaltyScore > 3.5) teamStats.PenaltyOver35Count++;
            if (penaltyScore > 4.5) teamStats.PenaltyOver45Count++;
        }

        private static double Rate(int count, int total) =>
            total == 0 ? 0 : (double)count / total * 100;

        private sealed record MatchStatsSource(
            DateTime Date,
            string Time,
            string League,
            string HomeTeam,
            string AwayTeam,
            int GoalHome,
            int GoalAway,
            int CornerHome,
            int CornerAway,
            int YellowHome,
            int YellowAway,
            int RedHome,
            int RedAway);

        private sealed class TeamStatsAccumulator
        {
            public int MatchCount { get; set; }
            public int GoalOver15Count { get; set; }
            public int GoalOver25Count { get; set; }
            public int GoalOver35Count { get; set; }
            public int CornerOver85Count { get; set; }
            public int CornerOver95Count { get; set; }
            public int CornerOver105Count { get; set; }
            public int PenaltyOver25Count { get; set; }
            public int PenaltyOver35Count { get; set; }
            public int PenaltyOver45Count { get; set; }
        }
    }
}
