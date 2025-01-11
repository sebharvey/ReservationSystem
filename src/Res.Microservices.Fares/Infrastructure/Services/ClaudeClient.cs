using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Res.Microservices.Fares.Application.Interfaces;

namespace Res.Microservices.Fares.Infrastructure.Services
{
    public class ClaudeClient : IClaudeClient
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://api.anthropic.com/v1/messages";
        private const string MODEL = "claude-3-sonnet-20240229";

        public ClaudeClient(string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<string> GetPricingResponse(string prompt)
        {
            var requestBody = new
            {
                model = MODEL,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 2048
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(API_URL, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var responseText = responseObject.GetProperty("content").GetProperty("0").GetProperty("text").GetString();

            var match = Regex.Match(responseText, @"\{[^{}]*\{.*\}[^{}]*\}", RegexOptions.Singleline);
            if (!match.Success)
                throw new Exception("Could not parse pricing response");

            return match.Value;
        }
    }
}