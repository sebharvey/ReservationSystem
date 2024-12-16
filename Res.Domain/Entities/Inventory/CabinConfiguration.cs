namespace Res.Domain.Entities.Inventory
{
    public class CabinConfiguration
    {
        public string CabinCode { get; set; } // J, W, Y
        public string CabinName { get; set; } // Business, Premium Economy, Economy
        public int FirstRow { get; set; }
        public int LastRow { get; set; }
        public List<string> SeatLetters { get; set; } // A,B,C,D,E,F etc
        public List<int> ExitRows { get; set; } = new();
        public List<int> BulkheadRows { get; set; } = new();
        public List<int> GalleryRows { get; set; } = new();
        public List<BlockedSeat> BlockedSeats { get; set; } = new();
        public Dictionary<string, BaseSeatDefinition> SeatDefinitions { get; set; } = new();
    }
}