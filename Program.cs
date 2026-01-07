using FutbolSitesi.Data;
using Microsoft.EntityFrameworkCore;
using FutbolSitesi.Models;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o =>
{
    o.AddPolicy("all", p =>
    {
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader();
    });
});


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=futbol.db"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

app.UseHttpsRedirection();

// CORS KULLAN
app.UseCors("all");

app.MapControllers();

// Seed example data into futbol.db if Matches table is empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Matches.Any())
    {
        db.Matches.Add(new Match
    {
        Season = "2024-25",
        League = "SuperLig",
        Week = 1,
        HomeTeam = "Galatasaray",
        AwayTeam = "Fenerbahce",
        Winner = "Home",
        GoalHome = 2,
        GoalAway = 1,
        CornerHome = 5,
        CornerAway = 3,
        YellowHome = 1,
        YellowAway = 2,
        RedHome = 0,
        RedAway = 0
    });
        db.SaveChanges();
    }
}

app.Run();
