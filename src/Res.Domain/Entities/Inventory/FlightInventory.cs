namespace Res.Domain.Entities.Inventory
{
    public class FlightInventory
    {
        public string FlightNo { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string DepartureDate { get; set; }
        public string ArrivalDate { get; set; }
        public Dictionary<string, int> Seats { get; set; }
        public string DepartureTime { get; set; }
        public string ArrivalTime { get; set; }
        public string AircraftType { get; set; }
    }
}