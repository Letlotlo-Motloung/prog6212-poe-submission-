using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services
{
    /// <summary>
    /// Single gateway between the MVC front-end and the Part 2 API.
    /// The MVC project never touches the database directly - every data operation goes through here.
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _httpContext;
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public ApiClient(HttpClient http, IHttpContextAccessor httpContext)
        {
            _http = http;
            _httpContext = httpContext;
        }

        private void AttachToken()
        {
            var token = _httpContext.HttpContext?.Session.GetString(SessionKeys.Token);
            _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }

        // ---------------- Generic helpers ----------------
        public async Task<ApiResult<T>> GetAsync<T>(string url)
        {
            AttachToken();
            try
            {
                var response = await _http.GetAsync(url);
                return await Read<T>(response);
            }
            catch (Exception ex) { return ApiResult<T>.Fail("Could not reach the API: " + ex.Message); }
        }

        public async Task<ApiResult<T>> PostAsync<T>(string url, object? body = null)
        {
            AttachToken();
            try
            {
                var response = await _http.PostAsJsonAsync(url, body ?? new { });
                return await Read<T>(response);
            }
            catch (Exception ex) { return ApiResult<T>.Fail("Could not reach the API: " + ex.Message); }
        }

        public async Task<ApiResult<T>> PutAsync<T>(string url, object body)
        {
            AttachToken();
            try
            {
                var response = await _http.PutAsJsonAsync(url, body);
                return await Read<T>(response);
            }
            catch (Exception ex) { return ApiResult<T>.Fail("Could not reach the API: " + ex.Message); }
        }

        public async Task<ApiResult<bool>> DeleteAsync(string url)
        {
            AttachToken();
            try
            {
                var response = await _http.DeleteAsync(url);
                if (response.IsSuccessStatusCode) return ApiResult<bool>.Ok(true, (int)response.StatusCode);
                var msg = await ExtractError(response);
                return ApiResult<bool>.Fail(msg, (int)response.StatusCode);
            }
            catch (Exception ex) { return ApiResult<bool>.Fail("Could not reach the API: " + ex.Message); }
        }

        private static async Task<ApiResult<T>> Read<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                if (typeof(T) == typeof(string))
                    return ApiResult<T>.Ok((T)(object)await response.Content.ReadAsStringAsync(), (int)response.StatusCode);

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOpts);
                return ApiResult<T>.Ok(data!, (int)response.StatusCode);
            }
            var error = await ExtractError(response);
            return ApiResult<T>.Fail(error, (int)response.StatusCode);
        }

        private static async Task<string> ExtractError(HttpResponseMessage response)
        {
            var raw = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(raw)) return $"Request failed ({(int)response.StatusCode}).";
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("title", out var title))
                    return title.GetString() ?? raw;
                if (doc.RootElement.TryGetProperty("detail", out var detail))
                    return detail.GetString() ?? raw;
            }
            catch { /* not JSON */ }
            return raw.Trim('"');
        }

        // ---------------- Auth ----------------
        public Task<ApiResult<AuthResponseDto>> LoginAsync(LoginDto dto) =>
            PostAsync<AuthResponseDto>("api/auth/login", new { dto.Email, dto.Password });

        public Task<ApiResult<AuthResponseDto>> RegisterAsync(RegisterDto dto) =>
            PostAsync<AuthResponseDto>("api/auth/register", new { dto.FullName, dto.Email, dto.Password, dto.PhoneNumber, dto.Role });

        // ---------------- Events ----------------
        public Task<ApiResult<List<EventDto>>> GetEventsAsync(string? search = null, string? type = null, string? location = null, bool upcomingOnly = false)
        {
            var q = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) q.Add("search=" + Uri.EscapeDataString(search));
            if (!string.IsNullOrWhiteSpace(type) && type != "All") q.Add("type=" + Uri.EscapeDataString(type));
            if (!string.IsNullOrWhiteSpace(location) && location != "All") q.Add("location=" + Uri.EscapeDataString(location));
            if (upcomingOnly) q.Add("upcomingOnly=true");
            var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
            return GetAsync<List<EventDto>>("api/events" + qs);
        }

        public Task<ApiResult<EventDetailsDto>> GetEventAsync(int id) =>
            GetAsync<EventDetailsDto>($"api/events/{id}");

        public Task<ApiResult<OrganiserDashboardDto>> GetDashboardAsync() =>
            GetAsync<OrganiserDashboardDto>("api/events/dashboard");

        public Task<ApiResult<List<OrganiserEventSummaryDto>>> GetMyEventsAsync() =>
            GetAsync<List<OrganiserEventSummaryDto>>("api/events/mine");

        public Task<ApiResult<EventDto>> CreateEventAsync(EventDto dto) =>
            PostAsync<EventDto>("api/events", dto);

        public Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventDto dto) =>
            PutAsync<EventDto>($"api/events/{id}", dto);

        public Task<ApiResult<bool>> DeleteEventAsync(int id) =>
            DeleteAsync($"api/events/{id}");

        // ---------------- Categories ----------------
        public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId) =>
            GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");

        public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryDto dto) =>
            PostAsync<CategoryDto>($"api/events/{eventId}/categories", dto);

        public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int eventId, int categoryId, CategoryDto dto) =>
            PutAsync<CategoryDto>($"api/events/{eventId}/categories/{categoryId}", dto);

        public Task<ApiResult<bool>> DeleteCategoryAsync(int eventId, int categoryId) =>
            DeleteAsync($"api/events/{eventId}/categories/{categoryId}");

        // ---------------- Enrolments ----------------
        public Task<ApiResult<object>> EnrolAsync(int categoryId) =>
            PostAsync<object>($"api/categories/{categoryId}/enrolments");

        public Task<ApiResult<List<MyEnrolmentDto>>> GetMyEnrolmentsAsync() =>
            GetAsync<List<MyEnrolmentDto>>("api/users/me/enrolments");

        public Task<ApiResult<bool>> CancelEnrolmentAsync(int enrolmentId) =>
            DeleteAsync($"api/enrolments/{enrolmentId}");

        public Task<ApiResult<List<EventEnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
            GetAsync<List<EventEnrolmentDto>>($"api/events/{eventId}/enrolments");

        public Task<ApiResult<List<EventEnrolmentDto>>> GetEventResultsAsync(int eventId) =>
            GetAsync<List<EventEnrolmentDto>>($"api/events/{eventId}/results");

        public Task<ApiResult<object>> CaptureResultAsync(int enrolmentId, ResultDto dto) =>
            PostAsync<object>($"api/enrolments/{enrolmentId}/results", dto);

        public Task<ApiResult<object>> UpdateResultAsync(int resultId, ResultDto dto) =>
            PutAsync<object>($"api/results/{resultId}", dto);

        public Task<ApiResult<object>> PublishResultsAsync(int eventId) =>
            PostAsync<object>($"api/events/{eventId}/publish-results");

        // ---------------- Participant results ----------------
        public Task<ApiResult<List<MyResultDto>>> GetMyResultsAsync() =>
            GetAsync<List<MyResultDto>>("api/users/me/results");

        // ---------------- Profile ----------------
        public Task<ApiResult<ProfileDto>> GetProfileAsync() =>
            GetAsync<ProfileDto>("api/users/me");

        public Task<ApiResult<object>> UpdateProfileAsync(UpdateProfileDto dto) =>
            PutAsync<object>("api/users/me", dto);

        public Task<ApiResult<object>> ChangePasswordAsync(UpdatePasswordDto dto) =>
            PutAsync<object>("api/users/me/password", dto);
    }
}
