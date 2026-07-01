using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace Flexfit.Helpers
{
    public static class ImageHelper
    {
        public static string? SaveBase64Image(string? base64Str, string subFolder, string fileNamePrefix, IWebHostEnvironment webHostEnvironment)
        {
            if (string.IsNullOrEmpty(base64Str)) return null;

            string base64Data = base64Str;
            string extension = "jpg"; // Mặc định

            if (base64Str.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = base64Str.IndexOf(',');
                if (commaIndex >= 0)
                {
                    var header = base64Str.Substring(0, commaIndex);
                    base64Data = base64Str.Substring(commaIndex + 1);

                    // Trích xuất định dạng ảnh
                    if (header.Contains("png")) extension = "png";
                    else if (header.Contains("gif")) extension = "gif";
                    else if (header.Contains("webp")) extension = "webp";
                    else if (header.Contains("jpeg") || header.Contains("jpg")) extension = "jpg";
                }
            }
            else
            {
                // Kiểm tra xem chuỗi có phải là base64 hợp lệ không
                Span<byte> buffer = new Span<byte>(new byte[base64Str.Length]);
                if (!Convert.TryFromBase64String(base64Str, buffer, out _))
                {
                    // Không phải base64, có thể là URL hoặc đường dẫn đã lưu. Trả về nguyên bản.
                    return base64Str;
                }
            }

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64Data);
                string webRootPath = webHostEnvironment.WebRootPath;
                if (string.IsNullOrEmpty(webRootPath))
                {
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }

                string uploadsFolder = Path.Combine(webRootPath, "uploads", subFolder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = $"{fileNamePrefix}_{Guid.NewGuid()}.{extension}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                File.WriteAllBytes(filePath, imageBytes);

                return $"/uploads/{subFolder}/{fileName}";
            }
            catch
            {
                // Gặp lỗi thì trả về chuỗi gốc để tránh mất dữ liệu
                return base64Str;
            }
        }

        public static string? GetAbsoluteUrl(string? relativeOrAbsoluteUrl, IHttpContextAccessor httpContextAccessor)
        {
            if (string.IsNullOrEmpty(relativeOrAbsoluteUrl)) return relativeOrAbsoluteUrl;
            if (relativeOrAbsoluteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                relativeOrAbsoluteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                relativeOrAbsoluteUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return relativeOrAbsoluteUrl;
            }

            var request = httpContextAccessor.HttpContext?.Request;
            if (request == null) return relativeOrAbsoluteUrl;

            return $"{request.Scheme}://{request.Host}{relativeOrAbsoluteUrl}";
        }
    }
}
