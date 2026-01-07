using Microsoft.EntityFrameworkCore;
using FutbolSitesi.Models;

namespace FutbolSitesi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Match> Matches { get; set; }
        public DbSet<Standing> Standings { get; set; }
    }
}
