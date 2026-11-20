using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Models
{
    public class User
    {
        [Key] public int UserID { get; set; }
        public int RoleID { get; set; }
        [ForeignKey("RoleID")] public Role? Role { get; set; }

        [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
        [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        [JsonIgnore] // Never send the BCrypt hash to a client
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)] public string? PhoneNumber { get; set; }

        /// <summary>Relative URL of the uploaded profile picture (stored by the MVC front-end).</summary>
        [MaxLength(500)] public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Event>? Events { get; set; }
        public ICollection<Enrolment>? Enrolments { get; set; }
    }
}
