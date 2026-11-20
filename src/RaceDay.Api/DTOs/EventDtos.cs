using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs
{
    public class EventDto
    {
        public int EventID { get; set; }
        [Required] public string EventName { get; set; } = string.Empty;
        [Required] public DateTime EventDate { get; set; }
        [Required] public string Location { get; set; } = string.Empty;
        [Required] public decimal DistanceKM { get; set; }
        public string? EventType { get; set; }
        public string? RouteInfo { get; set; }
        public string? Description { get; set; }
        public string? BannerUrl { get; set; }

        // Read-only projections
        public string? OrganiserName { get; set; }
        public int TotalEnrolments { get; set; }
        public int CategoryCount { get; set; }
        public bool IsPast { get; set; }
    }

    public class CategoryDto
    {
        public int CategoryID { get; set; }
        [Required] public string CategoryName { get; set; } = string.Empty;
        public string CategoryType { get; set; } = "Distance";
        [Required] public decimal DistanceKM { get; set; }
        public int MaxParticipants { get; set; } = 100;
        public decimal EntryFee { get; set; } = 0;

        // Read-only projections
        public int EnrolmentCount { get; set; }
    }

    public class ResultDto
    {
        [Required] public TimeSpan FinishTime { get; set; }
        [Required] public int Position { get; set; }
        [Required] public string Status { get; set; } = "Finished";
        public bool IsPublished { get; set; } = false;
    }

    public class MyEnrolmentDto
    {
        public int EnrolmentID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryType { get; set; } = "Distance";
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public bool HasResult { get; set; }
    }

    public class MyResultDto
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? FinishTime { get; set; }
        public int? Position { get; set; }
        public int TotalFinishers { get; set; }
        public string Status { get; set; } = "Finished";
    }

    public class EventEnrolmentDto
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryType { get; set; } = "Distance";
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public bool HasResult { get; set; }
        public int? ResultID { get; set; }
        public string? FinishTime { get; set; }
        public int? Position { get; set; }
        public bool IsPublished { get; set; }
    }

    public class OrganiserEventSummaryDto
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? EventType { get; set; }
        public string? BannerUrl { get; set; }
        public int CategoryCount { get; set; }
        public int TotalEnrolments { get; set; }
        public int ResultsCaptured { get; set; }
        public bool IsPast { get; set; }
    }

    public class OrganiserDashboardDto
    {
        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int TotalEnrolments { get; set; }
        public int TotalParticipants { get; set; }
        public int ResultsPublished { get; set; }
        public List<OrganiserEventSummaryDto> Events { get; set; } = new();
        public List<OrganiserEventSummaryDto> Upcoming { get; set; } = new();
    }
}
