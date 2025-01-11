namespace Res.Microservices.Fares.Application.DTOs
{
    public class FlightPricingDto
    {
        public string FlightNumber { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public Dictionary<string, CabinPricingDto> Cabins { get; set; }
    }
}