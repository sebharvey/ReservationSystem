using Res.Domain.Entities.Inventory;
using Res.Infrastructure.Interfaces;

namespace Res.Infrastructure.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private static List<FlightInventory> _inventories = new();
        private static Dictionary<string, FlightSeatInventory> _seatInventories = new();

        public List<FlightInventory> Inventory
        {
            get => _inventories;
            set => _inventories = value;
        }

        public Dictionary<string, FlightSeatInventory> SeatInventory
        {
            get => _seatInventories;
            set => _seatInventories = value;
        }
    }
}