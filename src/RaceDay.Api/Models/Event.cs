using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class Event
    {
        [Key] public int EventID { get; set; }
        public int OrganiserID { get; set; }
        [ForeignKey("OrganiserID")] public User? Organiser { get; set; }

        [Required, MaxLength(150)] public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        [Required, MaxLength(150)] public string Location { get; set; } = string.Empty;
        [Column(TypeName = "decimal(6,2)")] public decimal DistanceKM { get; set; }
        [MaxLength(50)] public string? EventType { get; set; }
        [MaxLength(255)] public string? RouteInfo { get; set; }
        public string? Description { get; set; }

        /// <summary>Relative URL of the uploaded event banner image (stored by the MVC front-end).</summary>
        [MaxLength(500)] public string? BannerUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Category>? Categories { get; set; }
    }
}
