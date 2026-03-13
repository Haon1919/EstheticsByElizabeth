using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using API.Data;
using System.Text.RegularExpressions;
using System.Linq;

namespace API.Services
{
    public class LlmService : ILlmService
    {
        private readonly ILogger<LlmService> _logger;
        private readonly ProjectContext _context;
        private readonly HttpClient _httpClient;

        public LlmService(ILogger<LlmService> logger, ProjectContext context, HttpClient httpClient)
        {
            _logger = logger;
            _context = context;
            _httpClient = httpClient;
        }

        private string SanitizeLlmInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Truncate to 2000 characters
            if (input.Length > 2000)
            {
                input = input.Substring(0, 2000);
            }

            // Remove control characters
            input = new string(input.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());

            // Strip obvious injection patterns
            var injectionPatterns = new[]
            {
                "ignore previous instructions",
                "ignore all previous instructions",
                "disregard previous instructions",
                "system prompt",
                "you are now",
                "act as",
                "```"
            };

            foreach (var pattern in injectionPatterns)
            {
                input = Regex.Replace(input, pattern, "[REDACTED]", RegexOptions.IgnoreCase);
            }

            // Reduce excessive newlines
            input = Regex.Replace(input, @"\n{3,}", "\n\n");

            return input;
        }

        private async Task<(string Provider, string ApiKey, string Endpoint, string Model)> GetSettingsAsync()
        {
            var provider = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "LlmProvider");
            var apiKey = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "LlmApiKey");
            var endpoint = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "LlmEndpoint");
            var model = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "LlmModel");

            return (
                provider?.Value ?? "Gemini",
                apiKey?.Value ?? "",
                endpoint?.Value ?? "",
                model?.Value ?? (provider?.Value == "Azure" ? "gpt-4o" : "gemini-2.5-flash")
            );
        }

        private async Task<string> CallLlmAsync(string systemPrompt, string userPrompt)
        {
            var settings = await GetSettingsAsync();
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                _logger.LogWarning("LLM API Key is not configured.");
                return "";
            }

            try
            {
                if (settings.Provider == "Azure")
                {
                    return await CallAzureOpenAIAsync(settings.ApiKey, settings.Endpoint, settings.Model, systemPrompt, userPrompt);
                }
                else
                {
                    return await CallGeminiAsync(settings.ApiKey, settings.Model, systemPrompt, userPrompt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling LLM API");
                return "";
            }
        }

        private async Task<string> CallGeminiAsync(string apiKey, string model, string systemPrompt, string userPrompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            
            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[]
                {
                    new { parts = new[] { new { text = userPrompt } } }
                },
                generationConfig = new { temperature = 0.7 }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "";
        }

        private async Task<string> CallAzureOpenAIAsync(string apiKey, string endpoint, string model, string systemPrompt, string userPrompt)
        {
            // Azure OpenAI REST API format
            var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{model}/chat/completions?api-version=2024-02-15-preview";
            
            var payload = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return text ?? "";
        }

        public async Task<(string Category, string DraftResponse, int UrgencyScore)> AnalyzeContactSubmissionAsync(string subject, string message)
        {
            var systemPrompt = @"You are an AI assistant for an esthetics business (Esthetics by Elizabeth). 
Analyze the following contact form submission. 
Return a JSON object with exactly these three keys:
1. ""Category"": A short category (e.g., ""Pricing Question"", ""Rescheduling"", ""Complaint"", ""General Inquiry"").
2. ""DraftResponse"": A polite, professional draft email response to the client.
3. ""UrgencyScore"": An integer from 1 to 10, where 10 is extremely urgent (e.g., angry complaint, immediate cancellation) and 1 is low urgency (e.g., general question).
Do not include markdown formatting like ```json in the output, just the raw JSON.";

            var sanitizedSubject = SanitizeLlmInput(subject);
            var sanitizedMessage = SanitizeLlmInput(message);
            var userPrompt = $"Subject: {sanitizedSubject}\nMessage: {sanitizedMessage}";

            var result = await CallLlmAsync(systemPrompt, userPrompt);
            if (string.IsNullOrEmpty(result)) return ("Uncategorized", "", 1);

            try
            {
                // Clean up potential markdown formatting
                result = result.Replace("```json", "").Replace("```", "").Trim();
                using var doc = JsonDocument.Parse(result);
                var category = doc.RootElement.GetProperty("Category").GetString() ?? "Uncategorized";
                var draftResponse = doc.RootElement.GetProperty("DraftResponse").GetString() ?? "";
                var urgencyScore = doc.RootElement.GetProperty("UrgencyScore").GetInt32();
                return (category, draftResponse, urgencyScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse LLM response for contact submission");
                return ("Uncategorized", "", 1);
            }
        }

        public async Task<(string Description, string Aftercare)> GenerateServiceContentAsync(string serviceName, decimal price)
        {
            var systemPrompt = @"You are an expert esthetician and copywriter. 
Generate content for a new service being offered.
Return a JSON object with exactly these two keys:
1. ""Description"": An enticing, professional, and SEO-friendly description of the service (2-3 paragraphs).
2. ""Aftercare"": Clear, medically accurate aftercare instructions for the client (bullet points).
Do not include markdown formatting like ```json in the output, just the raw JSON.";

            var sanitizedServiceName = SanitizeLlmInput(serviceName);
            var userPrompt = $"Service Name: {sanitizedServiceName}\nPrice: ${price}";

            var result = await CallLlmAsync(systemPrompt, userPrompt);
            if (string.IsNullOrEmpty(result)) return ("", "");

            try
            {
                result = result.Replace("```json", "").Replace("```", "").Trim();
                using var doc = JsonDocument.Parse(result);
                var description = doc.RootElement.GetProperty("Description").GetString() ?? "";
                var aftercare = doc.RootElement.GetProperty("Aftercare").GetString() ?? "";
                return (description, aftercare);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse LLM response for service generation");
                return ("", "");
            }
        }

        public async Task<string> SummarizeClientHistoryAsync(int clientId)
        {
            // Fetch client history from DB
            var client = await _context.Clients
                .Include(c => c.Appointments)
                    .ThenInclude(a => a.Service)
                .Include(c => c.ReviewFlags)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null) return "Client not found.";

            var historyBuilder = new StringBuilder();
            historyBuilder.AppendLine($"Client: {client.FirstName} {client.LastName}");
            historyBuilder.AppendLine($"Total Appointments: {client.Appointments.Count}");
            foreach (var appt in client.Appointments.OrderByDescending(a => a.Time).Take(5))
            {
                historyBuilder.AppendLine($"- {appt.Time:yyyy-MM-dd}: {appt.Service.Name}");
            }
            historyBuilder.AppendLine($"Total Flags: {client.ReviewFlags.Count}");
            foreach (var flag in client.ReviewFlags)
            {
                historyBuilder.AppendLine($"- Flag ({flag.Status}): {flag.FlagReason}");
            }

            var systemPrompt = @"You are an AI assistant for an esthetics business. 
Summarize the following client history into a concise 2-3 sentence summary for the admin. 
Highlight any patterns (e.g., frequent bookings, multiple flags).";

            var result = await CallLlmAsync(systemPrompt, historyBuilder.ToString());
            return result;
        }
    }
}