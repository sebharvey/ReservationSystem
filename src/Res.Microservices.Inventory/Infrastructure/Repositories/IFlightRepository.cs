using Res.Microservices.Inventory.Application.DTOs;
using Res.Microservices.Inventory.Domain.Entities;

namespace Res.Microservices.Inventory.Infrastructure.Repositories
{
    public interface IFlightRepository
    {
        Task<List<Flight>> SearchFlights(DateTime date, string origin, string destination);
        Task<Flight> GetFlight(string flightNo, DateTime departureDate);
        Task ImportFlights(List<Flight> flights);
        Task<bool> AllocateSeat(SeatAllocationRequest request);
        Task<List<AllocationResponse>> GetSeatMap(string flightNo, DateTime departureDate);
        Task ImportAircraftConfig(List<AircraftConfig> aircraftConfigs);
        Task<int> DeleteOldFlights(DateTime cutoffDate);
    }
}