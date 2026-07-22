using FlexFit.Engagement.Service.DTOs.AI;
using FlexFit.Engagement.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexFit.Engagement.API.Infrastructure.AI
{
    public class GeminiAIClient : IAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiAIClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
            _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
        }

        public async Task<string> GenerateContentAsync(string prompt, List<AIChatMessage>? history = null)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "### ⚠️ Cấu hình API Chưa Sẵn Sàng\n\nQuản trị viên chưa cấu hình `Gemini:ApiKey` trong `appsettings.json`. Vui lòng thêm Gemini API Key để trải nghiệm tính năng AI.";
            }

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                var contents = new List<object>();

                if (history != null && history.Any())
                {
                    foreach (var msg in history)
                    {
                        contents.Add(new
                        {
                            role = msg.Role == "user" ? "user" : "model",
                            parts = new[] { new { text = msg.Content } }
                        });
                    }
                }

                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                });

                var payload = new { contents };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    return $"### ❌ Lỗi kết nối Gemini API\n\nHTTP {response.StatusCode} - {errorText}";
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textProp))
                {
                    return textProp.GetString() ?? "Không nhận được phản hồi hợp lệ từ AI.";
                }

                return "Không thể phân tích dữ liệu trả về từ Gemini AI.";
            }
            catch (Exception ex)
            {
                return $"### 💥 Lỗi không xác định\n\nĐã xảy ra lỗi: {ex.Message}";
            }
        }
    }
}
