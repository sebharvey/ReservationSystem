namespace Res.Microservices.Fares.Domain.Entities
{
    public class Flight
    {
        public string FlightNumber { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public Dictionary<string, CabinInventory> CabinInventory { get; set; }
    }
}