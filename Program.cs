using FutbolSitesi.Data;
using Microsoft.EntityFrameworkCore;

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

/* 🚪 PORT */
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

/* 🗄️ SQLITE – MEVCUT futbol.db */
var isDocker =
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

string dbPath;

if (isDocker)
{
    Directory.CreateDirectory("/app/data");
    dbPath = "/app/data/futbol.db";
}
else
{
    dbPath = Path.Combine(AppContext.BaseDirectory, "futbol.db");
}

Console.WriteLine("USING DB: " + dbPath);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}")
);

/* 🎮 CONTROLLERS */
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

/* ❌ HTTPS REDIRECTION YOK */
// app.UseHttpsRedirection();

app.UseCors("all");

app.MapControllers();

app.MapGet("/api/ping", () => Results.Ok("pong"));

app.Run();
