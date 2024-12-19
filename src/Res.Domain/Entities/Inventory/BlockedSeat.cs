namespace Res.Domain.Entities.Inventory
{
    public class BlockedSeat
    {
        public string SeatNumber { get; set; } // e.g., "12A"
        public string Reason { get; set; } // e.g., "Crew Rest", "Broken", "Equipment"
    }
}