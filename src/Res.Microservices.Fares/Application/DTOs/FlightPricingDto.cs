using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class FlightPricingDto
    {
        [JsonPropertyName("flightNumber")]
        public string FlightNumber { get; set; }

        [JsonPropertyName("departureTime")]
        public TimeSpan DepartureTime { get; set; }

        [JsonPropertyName("cabins")]
        public Dictionary<string, CabinPricingDto> Cabins { get; set; }
    }
}