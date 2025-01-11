using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class PricingRequestDto
    {
        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonPropertyName("travelDate")]
        public DateTime TravelDate { get; set; }

        [JsonPropertyName("flights")]
        public List<FlightRequestDto> Flights { get; set; }

        [JsonPropertyName("passengers")]
        public List<PassengerRequestDto> Passengers { get; set; }
    }
}