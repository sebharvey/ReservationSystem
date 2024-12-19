using Res.Domain.Entities.Inventory;

namespace Res.Infrastructure.Data
{
    public interface IFlightSeatInventoryDataManager
    {
        Task<bool> CreateSeatInventoryAsync(FlightSeatInventory seatInventory);
        Task<FlightSeatInventory> GetSeatInventoryAsync(string flightNumber, string departureDate);
        Task<bool> UpdateSeatInventoryAsync(FlightSeatInventory seatInventory);
        Task<bool> DeleteSeatInventoryAsync(string flightNumber, string departureDate);
        Task<bool> AssignSeatAsync(string flightNumber, string departureDate, string seatNumber);
        Task<bool> ReleaseSeatAsync(string flightNumber, string departureDate, string seatNumber);
        Task<bool> IsSeatAvailableAsync(string flightNumber, string departureDate, string seatNumber);
    }
}