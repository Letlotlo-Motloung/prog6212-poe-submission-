using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models
{
    // ---------- Auth ----------
    public class LoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }

    public class RegisterDto
    {
        [Required, Display(Name = "Full name")] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, MinLength(6), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm password")] public string ConfirmPassword { get; set; } = string.Empty;
        [Phone, Display(Name = "Phone number")] public string? PhoneNumber { get; set; }
        [Required] public string Role { get; set; } = "Participant";
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }

    // ---------- Events ----------
    public class EventDto
    {
        public int EventID { get; set; }
        [Required, Display(Name = "Event name")] public string EventName { get; set; } = string.Empty;
        [Required, DataType(DataType.Date), Display(Name = "Event date")] public DateTime EventDate { get; set; } = DateTime.Today.AddMonths(1);
        [Required] public string Location { get; set; } = string.Empty;
        [Required, Range(0.1, 1000), Display(Name = "Distance (km)")] public decimal DistanceKM { get; set; }
        [Display(Name = "Event type")] public string? EventType { get; set; }
        [Display(Name = "Route information")] public string? RouteInfo { get; set; }
        public string? Description { get; set; }
        public string? BannerUrl { get; set; }

        public string? OrganiserName { get; set; }
        public int TotalEnrolments { get; set; }
        public int CategoryCount { get; set; }
        public bool IsPast { get; set; }
    }

    public class EventDetailsDto
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string? OrganiserName { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKM { get; set; }
        public string? EventType { get; set; }
        public string? RouteInfo { get; set; }
        public string? Description { get; set; }
        public string? BannerUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPast { get; set; }
        public int TotalEnrolments { get; set; }
        public List<CategoryDto> Categories { get; set; } = new();
    }

    public class CategoryDto
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        [Required, Display(Name = "Category name")] public string CategoryName { get; set; } = string.Empty;
        [Display(Name = "Category type")] public string CategoryType { get; set; } = "Distance";
        [Range(0, 1000), Display(Name = "Distance (km)")] public decimal DistanceKM { get; set; }
        [Range(1, 100000), Display(Name = "Maximum participants")] public int MaxParticipants { get; set; } = 100;
        [Range(0, 100000), Display(Name = "Entry fee (R)")] public decimal EntryFee { get; set; }
        public int EnrolmentCount { get; set; }
        public bool IsFull => EnrolmentCount >= MaxParticipants;
    }

    public class ResultDto
    {
        [Required, Display(Name = "Finish time (hh:mm:ss)")] public TimeSpan FinishTime { get; set; }
        [Required, Range(1, 100000)] public int Position { get; set; }
        public string Status { get; set; } = "Finished";
        public bool IsPublished { get; set; }
    }

    // ---------- Participant views ----------
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

    // ---------- Organiser views ----------
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

    // ---------- Profile ----------
    public class ProfileDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        [Required, Display(Name = "Full name")] public string FullName { get; set; } = string.Empty;
        [Phone, Display(Name = "Phone number")] public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class UpdatePasswordDto
    {
        [Required, DataType(DataType.Password), Display(Name = "Current password")] public string CurrentPassword { get; set; } = string.Empty;
        [Required, MinLength(6), DataType(DataType.Password), Display(Name = "New password")] public string NewPassword { get; set; } = string.Empty;
    }

    // ---------- Generic API result wrapper ----------
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
        public int StatusCode { get; set; }
        public static ApiResult<T> Ok(T data, int status = 200) => new() { Success = true, Data = data, StatusCode = status };
        public static ApiResult<T> Fail(string error, int status = 0) => new() { Success = false, Error = error, StatusCode = status };
    }
}
