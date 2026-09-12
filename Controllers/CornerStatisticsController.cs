using FutbolSitesi.Data;
using FutbolSitesi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FutbolSitesi.Controllers
{
    [ApiController]
    [Route("api/statistics")]
    public class CornerStatisticsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CornerStatisticsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("corners")]
        public async Task<ActionResult<IReadOnlyList<CornerStatisticsDto>>> GetCornerStatistics(
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
                    CornerHome = match.CornerHome,
                    CornerAway = match.CornerAway
                })
                .ToListAsync(cancellationToken);

            var teams = new Dictionary<string, Accumulator>(StringComparer.Ordinal);
            foreach (var match in matches)
            {
                var totalCorners = match.CornerHome + match.CornerAway;
                AddMatch(teams, match.HomeTeam, match.CornerHome, match.CornerAway, true, totalCorners);
                AddMatch(teams, match.AwayTeam, match.CornerAway, match.CornerHome, false, totalCorners);
            }

            var result = teams.Values
                .Select(ToDto)
                .OrderByDescending(team => team.Over85Rate)
                .Select((team, index) => { team.Rank = index + 1; return team; })
                .ToList();

            return Ok(result);
        }

        private static void AddMatch(
            IDictionary<string, Accumulator> teams,
            string teamName,
            int cornersFor,
            int cornersAgainst,
            bool isHome,
            int totalCorners)
        {
            if (!teams.TryGetValue(teamName, out var team))
            {
                team = new Accumulator { Name = teamName };
                teams[teamName] = team;
            }

            team.CornersFor += cornersFor;
            team.CornersAgainst += cornersAgainst;
            team.MatchCount++;
            team.TotalMatchCorners += totalCorners;
            if (isHome) team.HomeMatchCount++; else team.AwayMatchCount++;

            AddTotalThreshold(totalCorners > 7.5, team, 75);
            AddTotalThreshold(totalCorners > 8.5, team, 85);
            AddTotalThreshold(totalCorners > 9.5, team, 95);
            AddTotalThreshold(totalCorners > 10.5, team, 105);

            AddTeamThreshold(cornersFor > 3.5, team, isHome, 35);
            AddTeamThreshold(cornersFor > 4.5, team, isHome, 45);
            AddTeamThreshold(cornersFor > 5.5, team, isHome, 55);
            AddTeamThreshold(cornersFor > 6.5, team, isHome, 65);
            AddTeamThreshold(cornersFor > 7.5, team, isHome, 75);
            AddTeamThreshold(cornersFor > 8.5, team, isHome, 85);
        }

        private static void AddTotalThreshold(bool condition, Accumulator team, int threshold)
        {
            if (!condition) return;
            if (threshold == 75) team.Over75Count++;
            else if (threshold == 85) team.Over85Count++;
            else if (threshold == 95) team.Over95Count++;
            else team.Over105Count++;
        }

        private static void AddTeamThreshold(bool condition, Accumulator team, bool isHome, int threshold)
        {
            if (!condition) return;
            if (isHome)
            {
                if (threshold == 35) team.HomeOver35Count++; else if (threshold == 45) team.HomeOver45Count++; else if (threshold == 55) team.HomeOver55Count++; else if (threshold == 65) team.HomeOver65Count++; else if (threshold == 75) team.HomeOver75Count++; else team.HomeOver85Count++;
            }
            else
            {
                if (threshold == 35) team.AwayOver35Count++; else if (threshold == 45) team.AwayOver45Count++; else if (threshold == 55) team.AwayOver55Count++; else if (threshold == 65) team.AwayOver65Count++; else if (threshold == 75) team.AwayOver75Count++; else team.AwayOver85Count++;
            }
        }

        private static CornerStatisticsDto ToDto(Accumulator team)
        {
            double Rate(int count) => count / (double)team.MatchCount * 100;

            return new CornerStatisticsDto
            {
                Team = team.Name,
                CornersFor = team.CornersFor,
                CornersAgainst = team.CornersAgainst,
                HomeMatchCount = team.HomeMatchCount,
                AwayMatchCount = team.AwayMatchCount,
                MatchCount = team.MatchCount,
                TotalMatchCorners = team.TotalMatchCorners,
                Over75Count = team.Over75Count,
                Over85Count = team.Over85Count,
                Over95Count = team.Over95Count,
                Over105Count = team.Over105Count,
                HomeOver35Count = team.HomeOver35Count,
                AwayOver35Count = team.AwayOver35Count,
                HomeOver45Count = team.HomeOver45Count,
                AwayOver45Count = team.AwayOver45Count,
                HomeOver55Count = team.HomeOver55Count,
                AwayOver55Count = team.AwayOver55Count,
                HomeOver65Count = team.HomeOver65Count,
                AwayOver65Count = team.AwayOver65Count,
                HomeOver75Count = team.HomeOver75Count,
                AwayOver75Count = team.AwayOver75Count,
                HomeOver85Count = team.HomeOver85Count,
                AwayOver85Count = team.AwayOver85Count,
                AvgTeamCorners = team.CornersFor / (double)team.MatchCount,
                AvgMatchCorners = team.TotalMatchCorners / (double)team.MatchCount,
                Over75Rate = Rate(team.Over75Count),
                Over85Rate = Rate(team.Over85Count),
                Over95Rate = Rate(team.Over95Count),
                Over105Rate = Rate(team.Over105Count),
                Team35Rate = Rate(team.HomeOver35Count + team.AwayOver35Count),
                Team45Rate = Rate(team.HomeOver45Count + team.AwayOver45Count),
                Team55Rate = Rate(team.HomeOver55Count + team.AwayOver55Count),
                Team65Rate = Rate(team.HomeOver65Count + team.AwayOver65Count),
                Team75Rate = Rate(team.HomeOver75Count + team.AwayOver75Count),
                Team85Rate = Rate(team.HomeOver85Count + team.AwayOver85Count),
                AvgCornersUsed = team.CornersFor / (double)team.MatchCount
            };
        }

        private sealed class MatchData
        {
            public string HomeTeam { get; set; } = string.Empty;
            public string AwayTeam { get; set; } = string.Empty;
            public int CornerHome { get; set; }
            public int CornerAway { get; set; }
        }

        private sealed class Accumulator
        {
            public string Name { get; set; } = string.Empty;
            public int CornersFor { get; set; }
            public int CornersAgainst { get; set; }
            public int HomeMatchCount { get; set; }
            public int AwayMatchCount { get; set; }
            public int MatchCount { get; set; }
            public int TotalMatchCorners { get; set; }
            public int Over75Count { get; set; }
            public int Over85Count { get; set; }
            public int Over95Count { get; set; }
            public int Over105Count { get; set; }
            public int HomeOver35Count { get; set; }
            public int AwayOver35Count { get; set; }
            public int HomeOver45Count { get; set; }
            public int AwayOver45Count { get; set; }
            public int HomeOver55Count { get; set; }
            public int AwayOver55Count { get; set; }
            public int HomeOver65Count { get; set; }
            public int AwayOver65Count { get; set; }
            public int HomeOver75Count { get; set; }
            public int AwayOver75Count { get; set; }
            public int HomeOver85Count { get; set; }
            public int AwayOver85Count { get; set; }
        }
    }
}
