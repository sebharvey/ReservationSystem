using Res.Domain.Entities.Inventory;

namespace Res.Infrastructure.Interfaces
{
    public interface ISeatMapRepository
    {
        Dictionary<string, SeatConfiguration> SeatConfigurations { get; set; }
    }
}