using Res.Domain.Entities.Inventory;

namespace Res.Infrastructure.Data
{
    public interface IFlightInventoryDataManager
    {
        Task<bool> CreateFlightAsync(FlightInventory flight);
        Task<FlightInventory> GetFlightAsync(string flightNo, string departureDate);
        Task<bool> UpdateFlightAsync(FlightInventory flight);
        Task<bool> DeleteFlightAsync(string flightNo, string departureDate);
        Task<List<FlightInventory>> SearchAvailabilityAsync(string from, string to, string departureDate, string preferredTime = null);
        Task<bool> UpdateInventoryLevelsAsync(string flightNo, string departureDate, Dictionary<string, int> newLevels);
    }
}