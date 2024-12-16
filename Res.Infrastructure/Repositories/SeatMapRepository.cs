using Res.Domain.Entities.Inventory;
using Res.Infrastructure.Interfaces;

namespace Res.Infrastructure.Repositories
{
    public class SeatMapRepository : ISeatMapRepository
    {
        private static Dictionary<string, SeatConfiguration> _seatConfigurations = new();

        public Dictionary<string, SeatConfiguration> SeatConfigurations
        {
            get => _seatConfigurations;
            set => _seatConfigurations = value;
        }
    }
}