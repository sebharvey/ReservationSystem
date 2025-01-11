using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class CabinDetailsDto
    {
        [JsonPropertyName("cabinClass")]
        public string CabinClass { get; set; }

        [JsonPropertyName("totalSeats")]
        public int TotalSeats { get; set; }

        [JsonPropertyName("availableSeats")]
        public int AvailableSeats { get; set; }

        [JsonPropertyName("occupancyPercentage")]
        public double OccupancyPercentage { get; set; }

        [JsonPropertyName("demandLevel")]
        public string DemandLevel { get; set; }
    }
}