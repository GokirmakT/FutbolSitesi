using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using FutbolSitesi.Data;
using FutbolSitesi.Models;
using FutbolSitesi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FutbolSitesi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(AppDbContext db, JwtTokenService jwtTokenService)
        {
            _db = db;
            _jwtTokenService = jwtTokenService;
        }

        public class RegisterRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class AuthResponse
        {
            public string Token { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class MeResponse
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime? LastLogin { get; set; }
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username, email ve password zorunludur." });
            }

            var existingUser = await _db.Users
                .Where(u => u.Username == request.Username || u.Email == request.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return Conflict(new { message = "Bu kullanıcı adı veya email zaten kayıtlı." });
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _jwtTokenService.GenerateToken(user);

            var response = new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email
            };

            return Created("/api/auth/me", response);
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email ve password zorunludur." });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Geçersiz email veya şifre." });
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Geçersiz email veya şifre." });
            }

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var token = _jwtTokenService.GenerateToken(user);

            var response = new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email
            };

            return Ok(response);
        }

        // GET /api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                              User.FindFirst(ClaimTypes.Name) ??
                              User.FindFirst("sub");

            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Geçersiz token." });
            }

            if (!int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Geçersiz token." });
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı bulunamadı." });
            }

            var response = new MeResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };

            return Ok(response);
        }

        // GET /api/auth/users
        [HttpGet("users")]
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _db.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.CreatedAt,
                    u.LastLogin
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}

