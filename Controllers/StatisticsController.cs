using FutbolSitesi.Data;
using FutbolSitesi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FutbolSitesi.Controllers
{
    [ApiController]
    [Route("api/statistics")]
    public class StatisticsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StatisticsController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/statistics/cards?season=2026-2027&league=Super%20Lig
        [HttpGet("cards")]
        public async Task<ActionResult<IReadOnlyList<CardStatisticsDto>>> GetCardStatistics(
            [FromQuery] string season,
            [FromQuery] string league,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(season) || string.IsNullOrWhiteSpace(league))
            {
                return BadRequest("season ve league parametreleri zorunludur.");
            }

            var matches = await _db.Matches
                .AsNoTracking()
                .Where(match =>
                    match.Season == season &&
                    match.League == league &&
                    match.Winner != "TBD")
                .Select(match => new CardMatchData
                {
                    HomeTeam = match.HomeTeam,
                    AwayTeam = match.AwayTeam,
                    YellowHome = match.YellowHome,
                    YellowAway = match.YellowAway,
                    RedHome = match.RedHome,
                    RedAway = match.RedAway
                })
                .ToListAsync(cancellationToken);

            var teams = new Dictionary<string, CardAccumulator>(StringComparer.Ordinal);

            foreach (var match in matches)
            {
                var totalYellowCards = match.YellowHome + match.YellowAway;
                var totalRedCards = match.RedHome + match.RedAway;
                var totalPenaltyScore = match.YellowHome + match.RedHome * 2
                    + match.YellowAway + match.RedAway * 2;

                AddTeamMatch(
                    teams,
                    match.HomeTeam,
                    match.YellowHome,
                    match.RedHome,
                    match.YellowAway,
                    match.RedAway,
                    totalYellowCards,
                    totalRedCards,
                    totalPenaltyScore);

                AddTeamMatch(
                    teams,
                    match.AwayTeam,
                    match.YellowAway,
                    match.RedAway,
                    match.YellowHome,
                    match.RedHome,
                    totalYellowCards,
                    totalRedCards,
                    totalPenaltyScore);
            }

            var result = teams.Values
                .Select(ToDto)
                .OrderByDescending(team => team.Over25Rate)
                .Select((team, index) =>
                {
                    team.Rank = index + 1;
                    return team;
                })
                .ToList();

            return Ok(result);
        }

        private static void AddTeamMatch(
            IDictionary<string, CardAccumulator> teams,
            string teamName,
            int ownYellow,
            int ownRed,
            int opponentYellow,
            int opponentRed,
            int totalYellowCards,
            int totalRedCards,
            int totalPenaltyScore)
        {
            if (!teams.TryGetValue(teamName, out var team))
            {
                team = new CardAccumulator { Name = teamName };
                teams[teamName] = team;
            }

            team.YellowCards += ownYellow;
            team.RedCards += ownRed;
            team.MatchCount++;
            team.TotalMatchCards += ownYellow;
            team.OppYellow += opponentYellow;
            team.OppRed += opponentRed;

            if (totalYellowCards > 2.5) team.Over25Count++;
            if (totalYellowCards > 3.5) team.Over35Count++;
            if (totalYellowCards > 4.5) team.Over45Count++;
            if (totalYellowCards > 5.5) team.Over55Count++;

            if (totalRedCards > 0.5) team.RedOver05Count++;
            if (totalRedCards > 1.5) team.RedOver15Count++;
            if (totalRedCards > 2.5) team.RedOver25Count++;

            if (totalPenaltyScore > 2.5) team.PenaltyOver25Count++;
            if (totalPenaltyScore > 3.5) team.PenaltyOver35Count++;
            if (totalPenaltyScore > 4.5) team.PenaltyOver45Count++;
            if (totalPenaltyScore > 5.5) team.PenaltyOver55Count++;
        }

        private static CardStatisticsDto ToDto(CardAccumulator team)
        {
            var totalCardCount = team.YellowCards + team.RedCards
                + team.OppYellow + team.OppRed;
            var ownPenalty = team.YellowCards + team.RedCards * 2;
            var opponentPenalty = team.OppYellow + team.OppRed * 2;

            return new CardStatisticsDto
            {
                YellowCards = team.YellowCards,
                RedCards = team.RedCards,
                MatchCount = team.MatchCount,
                TotalMatchCards = team.TotalMatchCards,
                OppYellow = team.OppYellow,
                OppRed = team.OppRed,
                Over25Count = team.Over25Count,
                Over35Count = team.Over35Count,
                Over45Count = team.Over45Count,
                Over55Count = team.Over55Count,
                RedOver05Count = team.RedOver05Count,
                RedOver15Count = team.RedOver15Count,
                RedOver25Count = team.RedOver25Count,
                PenaltyOver25Count = team.PenaltyOver25Count,
                PenaltyOver35Count = team.PenaltyOver35Count,
                PenaltyOver45Count = team.PenaltyOver45Count,
                PenaltyOver55Count = team.PenaltyOver55Count,
                CardScore = ownPenalty,
                AvgCardScore = ownPenalty / (double)team.MatchCount,
                TotalCardCount = totalCardCount,
                AvgCardCount = totalCardCount / (double)team.MatchCount,
                AvgMatchCards = team.TotalMatchCards / (double)team.MatchCount,
                OwnPenalty = ownPenalty,
                OppPenalty = opponentPenalty,
                TotalRedCards = team.RedCards,
                Over25Rate = team.Over25Count / (double)team.MatchCount * 100,
                Over35Rate = team.Over35Count / (double)team.MatchCount * 100,
                Over45Rate = team.Over45Count / (double)team.MatchCount * 100,
                Over55Rate = team.Over55Count / (double)team.MatchCount * 100,
                RedOver05Rate = team.RedOver05Count / (double)team.MatchCount * 100,
                RedOver15Rate = team.RedOver15Count / (double)team.MatchCount * 100,
                RedOver25Rate = team.RedOver25Count / (double)team.MatchCount * 100,
                PenaltyOver25Rate = team.PenaltyOver25Count / (double)team.MatchCount * 100,
                PenaltyOver35Rate = team.PenaltyOver35Count / (double)team.MatchCount * 100,
                PenaltyOver45Rate = team.PenaltyOver45Count / (double)team.MatchCount * 100,
                PenaltyOver55Rate = team.PenaltyOver55Count / (double)team.MatchCount * 100,
                Team = team.Name
            };
        }

        private sealed class CardMatchData
        {
            public string HomeTeam { get; set; } = string.Empty;
            public string AwayTeam { get; set; } = string.Empty;
            public int YellowHome { get; set; }
            public int YellowAway { get; set; }
            public int RedHome { get; set; }
            public int RedAway { get; set; }
        }

        private sealed class CardAccumulator
        {
            public string Name { get; set; } = string.Empty;
            public int YellowCards { get; set; }
            public int RedCards { get; set; }
            public int MatchCount { get; set; }
            public int TotalMatchCards { get; set; }
            public int OppYellow { get; set; }
            public int OppRed { get; set; }
            public int Over25Count { get; set; }
            public int Over35Count { get; set; }
            public int Over45Count { get; set; }
            public int Over55Count { get; set; }
            public int RedOver05Count { get; set; }
            public int RedOver15Count { get; set; }
            public int RedOver25Count { get; set; }
            public int PenaltyOver25Count { get; set; }
            public int PenaltyOver35Count { get; set; }
            public int PenaltyOver45Count { get; set; }
            public int PenaltyOver55Count { get; set; }
        }
    }
}