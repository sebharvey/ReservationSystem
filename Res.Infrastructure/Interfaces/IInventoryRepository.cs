using Res.Domain.Entities.Inventory;

namespace Res.Infrastructure.Interfaces
{
    public interface IInventoryRepository
    {
        public List<FlightInventory> Inventory { get; set; }
        public Dictionary<string, FlightSeatInventory> SeatInventory { get; set; }
    }
}