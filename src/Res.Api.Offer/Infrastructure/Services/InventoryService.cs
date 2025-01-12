using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Res.Api.Offer.Domain.Interfaces;
using Res.Api.Offer.Domain.Models;

namespace Res.Api.Offer.Infrastructure.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public InventoryService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["InventoryApi:BaseUrl"];
        }

        public async Task<List<FlightAvailability>> GetAvailabilityAsync(string from, string to, DateTime date)
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/availability?from={from}&to={to}&date={date:yyyy-MM-dd}"
            );

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<FlightAvailability>>();
        }
    }
}