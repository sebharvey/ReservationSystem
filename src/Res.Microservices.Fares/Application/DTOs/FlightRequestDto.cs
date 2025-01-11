using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class FlightRequestDto
    {
        [JsonPropertyName("flightNumber")]
        public string FlightNumber { get; set; }

        [JsonPropertyName("departureTime")]
        public TimeSpan DepartureTime { get; set; }

        [JsonPropertyName("cabinInventory")]
        public Dictionary<string, CabinInventoryDto> CabinInventory { get; set; }
    }
}