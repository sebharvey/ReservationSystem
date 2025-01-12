namespace Res.Microservices.Inventory.Domain.Entities
{
    public class Allocation
    {
        public Guid Reference { get; set; }
        public Guid InventoryReference { get; set; }
        public string Seats { get; set; } 
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public virtual Flight Flight { get; set; }
    }
}