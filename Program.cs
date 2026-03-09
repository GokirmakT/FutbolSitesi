using FutbolSitesi.Data;
using FutbolSitesi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

/* 🔐 ENV (.env) YÜKLEME */
Env.Load();

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

/* 🔐 JWT AUTH */
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT secret not configured. Set JWT_SECRET in .env or Jwt:Key in appsettings.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtTokenService>();

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

/* 🗄️ USERS TABLOSU YOKSA OLUŞTUR (VERİ KAYBI YOK) */
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var createUsersSql = @"
CREATE TABLE IF NOT EXISTS ""Users"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT PK_Users PRIMARY KEY AUTOINCREMENT,
    ""Username"" TEXT NOT NULL,
    ""Email"" TEXT NOT NULL,
    ""Password"" TEXT NOT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    ""LastLogin"" TEXT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Username"" ON ""Users"" (""Username"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
";

    db.Database.ExecuteSqlRaw(createUsersSql);
}

app.UseCors("all");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/ping", () => Results.Ok("pong"));

app.Run();
