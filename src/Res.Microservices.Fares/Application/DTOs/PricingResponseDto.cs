
using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class PricingResponseDto
    {
        [JsonPropertyName("flights")]
        public List<FlightPricingDto> Flights { get; set; }
    }
}
