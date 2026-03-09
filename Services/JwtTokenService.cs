using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FutbolSitesi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FutbolSitesi.Services
{
    public class JwtTokenService
    {
        private readonly string _secret;
        private readonly int _expiresMinutes;

        public JwtTokenService(IConfiguration configuration)
        {
            // Öncelik .env içindeki JWT_SECRET ortam değişkeninde
            var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
            _secret = !string.IsNullOrWhiteSpace(envSecret)
                ? envSecret
                : configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT secret is not configured.");

            if (!int.TryParse(configuration["Jwt:ExpiresMinutes"], out _expiresMinutes))
            {
                _expiresMinutes = 60; // varsayılan: 60 dakika
            }
        }

        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiresMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

