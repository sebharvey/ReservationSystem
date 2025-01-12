namespace Res.Microservices.Inventory.Domain.Entities
{
    public class AircraftConfigBaseSeatDefinition
    {
        public bool IsWindow { get; set; }
        public bool IsAisle { get; set; }
        public bool IsMiddle { get; set; }
        public string Position { get; set; } // Left, Center, Right section of cabin
    }
}