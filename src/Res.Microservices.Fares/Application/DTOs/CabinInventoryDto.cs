using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class CabinInventoryDto
    {
        [JsonPropertyName("totalSeats")]
        public int TotalSeats { get; set; }

        [JsonPropertyName("availableSeats")]
        public int AvailableSeats { get; set; }
    }
}