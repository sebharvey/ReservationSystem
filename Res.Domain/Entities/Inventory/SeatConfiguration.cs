namespace Res.Domain.Entities.Inventory
{
    public class SeatConfiguration
    {
        public string AircraftType { get; set; }
        public Dictionary<string, CabinConfiguration> Cabins { get; set; } = new();
    }
}