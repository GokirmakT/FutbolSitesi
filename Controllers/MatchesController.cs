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
        
    }
}
