using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class PassengerPricingDto
    {
        [JsonPropertyName("passengerType")]
        public string PassengerType { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("baseFare")]
        public decimal BaseFare { get; set; }

        [JsonPropertyName("taxes")]
        public decimal Taxes { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }
    }
}