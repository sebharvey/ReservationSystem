using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class PricingFactorsDto
    {
        [JsonPropertyName("seasonality")]
        public string Seasonality { get; set; }

        [JsonPropertyName("demandLevel")]
        public string DemandLevel { get; set; }

        [JsonPropertyName("competitionLevel")]
        public string CompetitionLevel { get; set; }

        [JsonPropertyName("daysUntilDeparture")]
        public int DaysUntilDeparture { get; set; }

        [JsonPropertyName("cabinLoadFactor")]
        public double CabinLoadFactor { get; set; }

        [JsonPropertyName("timeOfDay")]
        public string TimeOfDay { get; set; }
    }
}