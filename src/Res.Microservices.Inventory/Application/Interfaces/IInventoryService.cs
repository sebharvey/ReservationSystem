using Res.Microservices.Inventory.Application.DTOs;

namespace Res.Microservices.Inventory.Application.Interfaces
{
    public interface IInventoryService
    {
        /// <summary>
        /// Searches for available flights based on search criteria
        /// </summary>
        /// <param name="request">Search request containing date, origin and destination</param>
        /// <returns>List of matching flights with availability</returns>
        Task<List<FlightSearchResponse>> SearchFlights(FlightSearchRequest request);

        /// <summary>
        /// Imports the flight schedule for a given date range
        /// </summary>
        /// <param name="request">Import request containing date range</param>
        Task ImportSchedule(ImportScheduleRequest request);

        /// <summary>
        /// Gets the seat map for a specific flight
        /// </summary>
        /// <param name="request">Request containing flight number and date</param>
        /// <returns>List of seats with their allocation status</returns>
        Task<List<AllocationResponse>> GetSeatMap(AllocationRequest request);

        /// <summary>
        /// Allocates a specific seat to a passenger
        /// </summary>
        /// <param name="request">Request containing allocation details</param>
        /// <returns>True if allocation successful, false otherwise</returns>
        Task<bool> AllocateSeat(SeatAllocationRequest request);
    }
}



