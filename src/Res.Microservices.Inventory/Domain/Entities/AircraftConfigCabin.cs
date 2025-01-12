namespace Res.Microservices.Inventory.Domain.Entities
{
    public class AircraftConfigCabin
    {
        public string CabinCode { get; set; } // J, W, Y
        public string CabinName { get; set; } // Business, Premium Economy, Economy
        public int FirstRow { get; set; }
        public int LastRow { get; set; }
        public List<string> SeatLetters { get; set; } // A,B,C,D,E,F etc
        public List<int> ExitRows { get; set; } = new();
        public List<int> BulkheadRows { get; set; } = new();
        public List<int> GalleryRows { get; set; } = new();
        public List<AircraftConfigBlockedSeat> BlockedSeats { get; set; } = new();
        public Dictionary<string, AircraftConfigBaseSeatDefinition> SeatDefinitions { get; set; } = new();
    }
}