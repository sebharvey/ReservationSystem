using Res.Domain.Entities.Inventory;

namespace Res.Infrastructure.Data
{
    public interface ISeatConfigurationDataManager
    {
        Task<bool> CreateConfigurationAsync(SeatConfiguration config);
        Task<SeatConfiguration> GetConfigurationAsync(string aircraftType);
        Task<bool> UpdateConfigurationAsync(SeatConfiguration config);
        Task<bool> DeleteConfigurationAsync(string aircraftType);
        Task<List<SeatConfiguration>> GetAllConfigurationsAsync();
        Task<bool> UpdateCabinAsync(string aircraftType, string cabinCode, CabinConfiguration cabinConfig);
    }
}