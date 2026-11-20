namespace RaceDay.Mvc.Services
{
    /// <summary>Stores uploaded images in the MVC wwwroot and returns a relative URL the API can persist.</summary>
    public static class FileUpload
    {
        private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxBytes = 5 * 1024 * 1024;

        public static async Task<(bool ok, string? url, string? error)> SaveImageAsync(IFormFile? file, IWebHostEnvironment env, string subfolder)
        {
            if (file == null || file.Length == 0) return (true, null, null);
            if (file.Length > MaxBytes) return (false, null, "Image must be smaller than 5 MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Allowed.Contains(ext)) return (false, null, "Only JPG, PNG, GIF or WEBP images are allowed.");

            var folder = Path.Combine(env.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);
            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return (true, $"/uploads/{subfolder}/{fileName}", null);
        }
    }
}
