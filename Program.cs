using FutbolSitesi.Data;
using Microsoft.EntityFrameworkCore;
using FutbolSitesi.Models;

var builder = WebApplication.CreateBuilder(args);

/* 🌍 CORS */
builder.Services.AddCors(o =>
{
    o.AddPolicy("all", p =>
    {
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader();
    });
});

/* 🚪 PORT OKU */
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

/* 🗄️ SQLITE */
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source=/app/futbol.db"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

/* 🔐 HTTPS SADECE LOCAL */
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("all");

app.MapControllers();

/* 🌱 SEED */
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
