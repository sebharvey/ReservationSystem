using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Res.Api.Offer.Domain.Interfaces;
using Res.Api.Offer.Domain.Models;

namespace Res.Api.Offer.Infrastructure.Services
{
    public class FaringService : IFaringService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public FaringService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["FaringApi:BaseUrl"];
        }

        public async Task<List<FlightPrice>> GetPricesAsync(List<string> flightNumbers, string currency)
        {
            var request = new { FlightNumbers = flightNumbers, Currency = currency };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/prices", request);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<FlightPrice>>();
        }
    }
}