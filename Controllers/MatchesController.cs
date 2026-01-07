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
    public class MatchesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MatchesController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/matches
        [HttpGet]
        public async Task<IActionResult> GetMatches()
        {
            var matches = await _db.Matches.ToListAsync();
            return Ok(matches);
        }

        // GET /api/matches/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMatchById(int id)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound();
            return Ok(match);
        }

        // POST /api/matches
        [HttpPost]
        public async Task<IActionResult> CreateMatch([FromBody] Match match)
        {
            _db.Matches.Add(match);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMatchById), new { id = match.Id }, match);
        }

        // PUT /api/matches/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMatch(int id, [FromBody] Match match)
        {
            var existingMatch = await _db.Matches.FindAsync(id);
            if (existingMatch == null) return NotFound();

            existingMatch.Season = match.Season;
            existingMatch.League = match.League;
            existingMatch.Week = match.Week;
            existingMatch.Date = match.Date;
            existingMatch.Time = match.Time;
            existingMatch.HomeTeam = match.HomeTeam;
            existingMatch.AwayTeam = match.AwayTeam;
            existingMatch.Winner = match.Winner;
            existingMatch.GoalHome = match.GoalHome;
            existingMatch.GoalAway = match.GoalAway;
            existingMatch.CornerHome = match.CornerHome;
            existingMatch.CornerAway = match.CornerAway;
            existingMatch.YellowHome = match.YellowHome;
            existingMatch.YellowAway = match.YellowAway;
            existingMatch.RedHome = match.RedHome;
            existingMatch.RedAway = match.RedAway;

            await _db.SaveChangesAsync();
            return Ok(existingMatch);
        }

        // DELETE /api/matches/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMatch(int id)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound();

            _db.Matches.Remove(match);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
