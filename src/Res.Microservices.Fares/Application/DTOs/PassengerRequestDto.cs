using System.Text.Json.Serialization;

namespace Res.Microservices.Fares.Application.DTOs
{
    public class PassengerRequestDto
    {
        [JsonPropertyName("ptcCode")]
        public string PtcCode { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}