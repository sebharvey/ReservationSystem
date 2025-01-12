namespace Res.Microservices.Inventory.Domain.Entities
{
    public class Flight
    {
        public Guid Reference { get; set; }
        public string FlightNo { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }
        public Dictionary<string, int> Seats { get; set; }  
        public string AircraftType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}