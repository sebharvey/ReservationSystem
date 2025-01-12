namespace Res.Microservices.Inventory.Application.DTOs
{
    public class FlightSearchResponse
    {
        public string FlightNo { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }
        public int ArrivalOffset { get; set; }
        public string Aircraft { get; set; }
        public List<CabinAvailability> Availability { get; set; } = new();

        public class CabinAvailability
        {
            public string Cabin { get; set; }
            public int Remaining { get; set; }
        }
    }
}