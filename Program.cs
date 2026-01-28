using FutbolSitesi.Data;
using FutbolSitesi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = Environments.Production   // 🔴 KRİTİK
});

/* 🔴 DOSYA IZLEMEYI KAPAT */
builder.Configuration.AddJsonFile(
    "appsettings.json",
    optional: false,
    reloadOnChange: false
);

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

/* 🚪 PORT */
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

/* 🗄️ SQLITE */
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=/app/data/futbol.db")
);

/* 🎮 CONTROLLERS */
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

/* ❌ HTTPS REDIRECTION YOK (Render zaten proxy) */
// app.UseHttpsRedirection();

app.UseCors("all");

app.MapControllers();

app.MapGet("/api/ping", () => Results.Ok("pong"));

/* 🌱 SEED (sadece ilk kez) */
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

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
