using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class CabinPricingDto
    {
        [JsonPropertyName("pricing")]
        public List<PassengerPricingDto> Pricing { get; set; }

        [JsonPropertyName("totalItinerary")]
        public decimal TotalItinerary { get; set; }

        [JsonPropertyName("cabinDetails")]
        public CabinDetailsDto CabinDetails { get; set; }

        [JsonPropertyName("pricingFactors")]
        public PricingFactorsDto Factors { get; set; }

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; }
    }
}