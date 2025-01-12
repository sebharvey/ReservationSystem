namespace Res.Microservices.Inventory.Application.DTOs
{
    public class FlightSearchRequest
    {
        public DateTime DepartureDate { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
    }
}