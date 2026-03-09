using FutbolSitesi.Data;
using FutbolSitesi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

/* 🔐 ENV (.env) YÜKLE */
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

/* 🗄️ DATABASE PATH */
string dbPath = Environment.GetEnvironmentVariable("DB_PATH");

if (string.IsNullOrEmpty(dbPath))
{
    // Local geliştirme
    var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
    Directory.CreateDirectory(dataDir);
    dbPath = Path.Combine(dataDir, "futbol.db");
}

Console.WriteLine("USING DB: " + dbPath);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}")
);

/* 🔐 JWT AUTH */
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT secret not configured.");

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

/* 🗄️ USERS TABLOSU YOKSA OLUŞTUR */
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