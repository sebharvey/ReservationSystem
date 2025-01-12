namespace Res.Microservices.Inventory.Application.DTOs
{
    public class AllocationResponse
    {
        public string Number { get; set; }
        public bool IsAvailable { get; set; }
        public string RecordLocator { get; set; }
        public int? PaxId { get; set; }
    }
}