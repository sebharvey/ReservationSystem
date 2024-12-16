namespace Res.Domain.Entities.Inventory
{
    public class FlightSeatInventory
    {
        public string FlightNumber { get; set; }
        public string DepartureDate { get; set; }
        public HashSet<string> OccupiedSeats { get; set; } = new();
        public string AircraftType { get; set; }
    }
}