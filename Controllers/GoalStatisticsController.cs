using FutbolSitesi.Data;
using FutbolSitesi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FutbolSitesi.Controllers
{
    [ApiController]
    [Route("api/statistics")]
    public class GoalStatisticsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public GoalStatisticsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("goals")]
        public async Task<ActionResult<IReadOnlyList<GoalStatisticsDto>>> GetGoalStatistics(
            [FromQuery] string season,
            [FromQuery] string league,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(season) || string.IsNullOrWhiteSpace(league))
                return BadRequest("season ve league parametreleri zorunludur.");

            var matches = await _db.Matches
                .AsNoTracking()
                .Where(match => match.Season == season && match.League == league && match.Winner != "TBD")
                .Select(match => new MatchData
                {
                    HomeTeam = match.HomeTeam,
                    AwayTeam = match.AwayTeam,
                    GoalHome = match.GoalHome,
                    GoalAway = match.GoalAway,
                    HomeGoalsMinutes = match.HomeGoalsMinutes,
                    AwayGoalsMinutes = match.AwayGoalsMinutes
                })
                .ToListAsync(cancellationToken);

            var teams = new Dictionary<string, Accumulator>(StringComparer.Ordinal);
            foreach (var match in matches)
            {
                var totalGoals = match.GoalHome + match.GoalAway;
                var bts = match.GoalHome > 0 && match.GoalAway > 0;
                var homeBothHalves = HasScoredBothHalves(match.HomeGoalsMinutes);
                var awayBothHalves = HasScoredBothHalves(match.AwayGoalsMinutes);

                AddMatch(teams, match.HomeTeam, match.GoalHome, match.GoalAway, true, totalGoals, bts, homeBothHalves);
                AddMatch(teams, match.AwayTeam, match.GoalAway, match.GoalHome, false, totalGoals, bts, awayBothHalves);
            }

            var result = teams.Values
                .Select(ToDto)
                .OrderByDescending(team => team.Over25Rate)
                .Select((team, index) => { team.Rank = index + 1; return team; })
                .ToList();

            return Ok(result);
        }

        private static void AddMatch(
            IDictionary<string, Accumulator> teams,
            string teamName,
            int goalsFor,
            int goalsAgainst,
            bool isHome,
            int totalGoals,
            bool bts,
            bool bothHalves)
        {
            if (!teams.TryGetValue(teamName, out var team))
            {
                team = new Accumulator { Name = teamName };
                teams[teamName] = team;
            }

            team.GoalsFor += goalsFor;
            team.GoalsAgainst += goalsAgainst;
            team.MatchCount++;
            team.TotalMatchGoals += totalGoals;
            if (isHome) team.HomeMatchCount++; else team.AwayMatchCount++;
            if (bts) { team.Bts++; if (isHome) team.HomeBts++; else team.AwayBts++; }
            if (bothHalves) { team.BothHalvesScored++; if (isHome) team.HomeBothHalvesScored++; else team.AwayBothHalvesScored++; }

            AddThreshold(totalGoals > 2.5, team, isHome, 25);
            AddThreshold(totalGoals > 3.5, team, isHome, 35);
            AddThreshold(totalGoals > 4.5, team, isHome, 45);
            AddThreshold(totalGoals > 1.5, team, isHome, 15);
            AddLessThreshold(totalGoals < 2.5, team, isHome, 25);
            AddLessThreshold(totalGoals < 3.5, team, isHome, 35);
            AddLessThreshold(totalGoals < 4.5, team, isHome, 45);
            AddLessThreshold(totalGoals < 1.5, team, isHome, 15);
        }

        private static void AddThreshold(bool condition, Accumulator team, bool isHome, int threshold)
        {
            if (!condition) return;
            if (threshold == 15) team.Over15Count++; else if (threshold == 25) team.Over25Count++; else if (threshold == 35) team.Over35Count++; else team.Over45Count++;
            if (isHome) { if (threshold == 15) team.HomeOver15Count++; else if (threshold == 25) team.HomeOver25Count++; else if (threshold == 35) team.HomeOver35Count++; else team.HomeOver45Count++; }
            else { if (threshold == 15) team.AwayOver15Count++; else if (threshold == 25) team.AwayOver25Count++; else if (threshold == 35) team.AwayOver35Count++; else team.AwayOver45Count++; }
        }

        private static void AddLessThreshold(bool condition, Accumulator team, bool isHome, int threshold)
        {
            if (!condition) return;
            if (threshold == 15) team.Less15Count++; else if (threshold == 25) team.Less25Count++; else if (threshold == 35) team.Less35Count++; else team.Less45Count++;
            if (isHome) { if (threshold == 15) team.HomeLess15Count++; else if (threshold == 25) team.HomeLess25Count++; else if (threshold == 35) team.HomeLess35Count++; else team.HomeLess45Count++; }
            else { if (threshold == 15) team.AwayLess15Count++; else if (threshold == 25) team.AwayLess25Count++; else if (threshold == 35) team.AwayLess35Count++; else team.AwayLess45Count++; }
        }

        private static bool HasScoredBothHalves(string? minutesText)
        {
            if (string.IsNullOrWhiteSpace(minutesText)) return false;
            var minutes = minutesText.Split('|').Select(ParseMinute).ToList();
            return minutes.Any(minute => minute <= 45) && minutes.Any(minute => minute >= 46);
        }

        private static int ParseMinute(string value)
        {
            var parts = value.Split('+');
            return int.TryParse(parts[0], out var baseMinute)
                ? baseMinute + (parts.Length > 1 && int.TryParse(parts[1], out var extra) ? extra : 0)
                : -1;
        }

        private static GoalStatisticsDto ToDto(Accumulator team)
        {
            double Rate(int count) => count / (double)team.MatchCount * 100;
            double HomeRate(int count) => team.HomeMatchCount == 0 ? 0 : count / (double)team.HomeMatchCount * 100;
            double AwayRate(int count) => team.AwayMatchCount == 0 ? 0 : count / (double)team.AwayMatchCount * 100;

            return new GoalStatisticsDto
            {
                Team = team.Name, GoalsFor = team.GoalsFor, GoalsAgainst = team.GoalsAgainst,
                HomeMatchCount = team.HomeMatchCount, AwayMatchCount = team.AwayMatchCount, MatchCount = team.MatchCount,
                TotalMatchGoals = team.TotalMatchGoals, Bts = team.Bts, HomeBts = team.HomeBts, AwayBts = team.AwayBts,
                BothHalvesScored = team.BothHalvesScored, HomeBothHalvesScored = team.HomeBothHalvesScored, AwayBothHalvesScored = team.AwayBothHalvesScored,
                Over25Count = team.Over25Count, HomeOver25Count = team.HomeOver25Count, AwayOver25Count = team.AwayOver25Count,
                Over35Count = team.Over35Count, HomeOver35Count = team.HomeOver35Count, AwayOver35Count = team.AwayOver35Count,
                Over45Count = team.Over45Count, HomeOver45Count = team.HomeOver45Count, AwayOver45Count = team.AwayOver45Count,
                Over15Count = team.Over15Count, HomeOver15Count = team.HomeOver15Count, AwayOver15Count = team.AwayOver15Count,
                Less25Count = team.Less25Count, HomeLess25Count = team.HomeLess25Count, AwayLess25Count = team.AwayLess25Count,
                Less35Count = team.Less35Count, HomeLess35Count = team.HomeLess35Count, AwayLess35Count = team.AwayLess35Count,
                Less45Count = team.Less45Count, HomeLess45Count = team.HomeLess45Count, AwayLess45Count = team.AwayLess45Count,
                Less15Count = team.Less15Count, HomeLess15Count = team.HomeLess15Count, AwayLess15Count = team.AwayLess15Count,
                AvgGoalsFor = team.GoalsFor / (double)team.MatchCount, AvgMatchGoals = team.TotalMatchGoals / (double)team.MatchCount,
                BtsRate = Rate(team.Bts), HomeBtsRate = HomeRate(team.HomeBts), AwayBtsRate = AwayRate(team.AwayBts),
                Over25Rate = Rate(team.Over25Count), Over35Rate = Rate(team.Over35Count), Over45Rate = Rate(team.Over45Count), Over15Rate = Rate(team.Over15Count),
                Less25Rate = Rate(team.Less25Count), Less35Rate = Rate(team.Less35Count), Less45Rate = Rate(team.Less45Count), Less15Rate = Rate(team.Less15Count),
                HomeOver25Rate = HomeRate(team.HomeOver25Count), AwayOver25Rate = AwayRate(team.AwayOver25Count), HomeOver35Rate = HomeRate(team.HomeOver35Count), AwayOver35Rate = AwayRate(team.AwayOver35Count),
                HomeOver45Rate = HomeRate(team.HomeOver45Count), AwayOver45Rate = AwayRate(team.AwayOver45Count), HomeOver15Rate = HomeRate(team.HomeOver15Count), AwayOver15Rate = AwayRate(team.AwayOver15Count),
                HomeLess25Rate = HomeRate(team.HomeLess25Count), AwayLess25Rate = AwayRate(team.AwayLess25Count), HomeLess35Rate = HomeRate(team.HomeLess35Count), AwayLess35Rate = AwayRate(team.AwayLess35Count),
                HomeLess45Rate = HomeRate(team.HomeLess45Count), AwayLess45Rate = AwayRate(team.AwayLess45Count), HomeLess15Rate = HomeRate(team.HomeLess15Count), AwayLess15Rate = AwayRate(team.AwayLess15Count),
                BothHalvesRate = Rate(team.BothHalvesScored), HomeBothHalvesRate = HomeRate(team.HomeBothHalvesScored), AwayBothHalvesRate = AwayRate(team.AwayBothHalvesScored)
            };
        }

        private sealed class MatchData
        {
            public string HomeTeam { get; set; } = string.Empty;
            public string AwayTeam { get; set; } = string.Empty;
            public int GoalHome { get; set; }
            public int GoalAway { get; set; }
            public string? HomeGoalsMinutes { get; set; }
            public string? AwayGoalsMinutes { get; set; }
        }

        private sealed class Accumulator
        {
            public string Name { get; set; } = string.Empty;
            public int GoalsFor { get; set; }
            public int GoalsAgainst { get; set; }
            public int HomeMatchCount { get; set; }
            public int AwayMatchCount { get; set; }
            public int MatchCount { get; set; }
            public int TotalMatchGoals { get; set; }
            public int Bts { get; set; }
            public int HomeBts { get; set; }
            public int AwayBts { get; set; }
            public int BothHalvesScored { get; set; }
            public int HomeBothHalvesScored { get; set; }
            public int AwayBothHalvesScored { get; set; }
            public int Over25Count { get; set; }
            public int HomeOver25Count { get; set; }
            public int AwayOver25Count { get; set; }
            public int Over35Count { get; set; }
            public int HomeOver35Count { get; set; }
            public int AwayOver35Count { get; set; }
            public int Over45Count { get; set; }
            public int HomeOver45Count { get; set; }
            public int AwayOver45Count { get; set; }
            public int Over15Count { get; set; }
            public int HomeOver15Count { get; set; }
            public int AwayOver15Count { get; set; }
            public int Less25Count { get; set; }
            public int HomeLess25Count { get; set; }
            public int AwayLess25Count { get; set; }
            public int Less35Count { get; set; }
            public int HomeLess35Count { get; set; }
            public int AwayLess35Count { get; set; }
            public int Less45Count { get; set; }
            public int HomeLess45Count { get; set; }
            public int AwayLess45Count { get; set; }
            public int Less15Count { get; set; }
            public int HomeLess15Count { get; set; }
            public int AwayLess15Count { get; set; }
        }
    }
}
