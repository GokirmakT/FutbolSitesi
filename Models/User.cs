using System;
using System.ComponentModel.DataAnnotations;

namespace FutbolSitesi.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        // Veritabanında hashlenmiş şifre saklanır
        [Required]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLogin { get; set; }
    }
}

