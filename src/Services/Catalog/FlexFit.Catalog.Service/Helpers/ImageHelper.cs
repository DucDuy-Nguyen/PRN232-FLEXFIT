using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace FlexFit.Catalog.Service.Helpers;

public static class ImageHelper
{
    public static string? SaveBase64Image(string? base64Str, string subFolder, string fileNamePrefix, IWebHostEnvironment webHostEnvironment)
    {
        if (string.IsNullOrEmpty(base64Str)) return null;

        string base64Data = base64Str;
        string extension = "jpg";

        if (base64Str.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = base64Str.IndexOf(',');
            if (commaIndex >= 0)
            {
                var header = base64Str.Substring(0, commaIndex);
                base64Data = base64Str.Substring(commaIndex + 1);

                if (header.Contains("png")) extension = "png";
                else if (header.Contains("gif")) extension = "gif";
                else if (header.Contains("webp")) extension = "webp";
                else if (header.Contains("jpeg") || header.Contains("jpg")) extension = "jpg";
            }
        }
        else
        {
            Span<byte> buffer = new Span<byte>(new byte[base64Str.Length]);
            if (!Convert.TryFromBase64String(base64Str, buffer, out _))
            {
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

