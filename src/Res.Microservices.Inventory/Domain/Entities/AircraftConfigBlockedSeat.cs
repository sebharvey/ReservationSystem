namespace Res.Microservices.Inventory.Domain.Entities
{
    public class AircraftConfigBlockedSeat
    {
        public string SeatNumber { get; set; } // e.g., "12A"
        public string Reason { get; set; } // e.g., "Crew Rest", "Broken", "Equipment"
    }
}