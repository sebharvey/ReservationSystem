using Res.Domain.Entities.Inventory;
using Res.Domain.Requests;

namespace Res.Core.Interfaces
{
    public interface IInventoryService
    {
        Task<List<FlightInventory>> SearchAvailability(AvailabilityRequest request);
        Task<FlightInventory> FindFlight(string flightNo, string date);
        void AddFlight(string flightFlightNo, string flightFrom, string flightTo, string departureDate, string arrivalDate, string departureTime, string arrivalTime, Dictionary<string, int> flightSeats, string aircraftType);
        bool IncrementInventory(string flightNumber, string date, string bookingClass, int quantity);
        bool DecrementInventory(string flightNumber, string date, string bookingClass, int quantity);
        Task<bool> IsValidSeat(string flightNumber, string departureDate, string seatNumber);
        Task<bool> IsSeatAvailable(string flightNumber, string departureDate, string seatNumber);
        Task<bool> AssignSeat(string flightNumber, string departureDate, string seatNumber);
        Task<bool> ReleaseSeat(string flightNumber, string departureDate, string seatNumber);
        Task<CabinConfiguration> GetCabinConfigForSeat(string aircraftType, string seatNumber);
        Task<List<FlightStatus>> FlightStatus(DateTime today);
    }
}