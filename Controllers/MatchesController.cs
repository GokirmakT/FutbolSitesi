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
        
    }
}
