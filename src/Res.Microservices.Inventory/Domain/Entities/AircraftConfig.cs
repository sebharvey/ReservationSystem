namespace Res.Microservices.Inventory.Domain.Entities
{
    public class AircraftConfig
    {
        public Guid Reference { get; set; }
        public string AircraftType { get; set; }
        public Dictionary<string, AircraftConfigCabin> Cabins { get; set; } = new();
    }
}