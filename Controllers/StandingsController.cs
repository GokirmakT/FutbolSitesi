using Microsoft.AspNetCore.Mvc;
using FutbolSitesi.Models;
using FutbolSitesi.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace FutbolSitesi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StandingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StandingsController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/standings
        [HttpGet]
        public async Task<IActionResult> GetStandings([FromQuery] string? league = null, [FromQuery] string? season = null)
        {
            var query = _db.Standings.AsQueryable();

            if (!string.IsNullOrEmpty(league))
            {
                query = query.Where(s => s.League == league);
            }

            if (!string.IsNullOrEmpty(season))
            {
                query = query.Where(s => s.Season == season);
            }

            var standings = await query
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.GoalDiff)
                .ThenByDescending(s => s.GoalFor)
                .ToListAsync();

            return Ok(standings);
        }

        // GET /api/standings/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStandingById(int id)
        {
            var standing = await _db.Standings.FindAsync(id);
            if (standing == null) return NotFound();
            return Ok(standing);
        }

        // GET /api/standings/league/{league}
        [HttpGet("league/{league}")]
        public async Task<IActionResult> GetStandingsByLeague(string league, [FromQuery] string? season = null)
        {
            var query = _db.Standings.Where(s => s.League == league);

            if (!string.IsNullOrEmpty(season))
            {
                query = query.Where(s => s.Season == season);
            }

            var standings = await query
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.GoalDiff)
                .ThenByDescending(s => s.GoalFor)
                .ToListAsync();

            return Ok(standings);
        }

        // POST /api/standings
        [HttpPost]
        public async Task<IActionResult> CreateStanding([FromBody] Standing standing)
        {
            _db.Standings.Add(standing);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetStandingById), new { id = standing.Id }, standing);
        }

        // PUT /api/standings/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStanding(int id, [FromBody] Standing standing)
        {
            var existingStanding = await _db.Standings.FindAsync(id);
            if (existingStanding == null) return NotFound();

            existingStanding.Season = standing.Season;
            existingStanding.League = standing.League;
            existingStanding.Team = standing.Team;
            existingStanding.Played = standing.Played;
            existingStanding.Win = standing.Win;
            existingStanding.Draw = standing.Draw;
            existingStanding.Lose = standing.Lose;
            existingStanding.GoalFor = standing.GoalFor;
            existingStanding.GoalAgainst = standing.GoalAgainst;
            existingStanding.GoalDiff = standing.GoalDiff;
            existingStanding.Points = standing.Points;

            await _db.SaveChangesAsync();
            return Ok(existingStanding);
        }

        // DELETE /api/standings/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStanding(int id)
        {
            var standing = await _db.Standings.FindAsync(id);
            if (standing == null) return NotFound();

            _db.Standings.Remove(standing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

