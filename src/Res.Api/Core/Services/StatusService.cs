using Res.Api.Models;
using Res.Core.Interfaces;
using Res.Domain.Entities.Inventory;

namespace Res.Api.Core.Services
{
    public class StatusService : IStatusService
    {
        private readonly IInventoryService _inventoryService;

        public StatusService(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public async Task<List<StatusResponse>> FlightStatus(DateTime date)
        {
            List<FlightStatus> flightStatus = await _inventoryService.FlightStatus(DateTime.Today);

            var flights = flightStatus.Select(flight => new StatusResponse
            {
                Aircraft = flight.Aircraft,
                From = flight.From,
                To = flight.To,
                DepartureDateTime = flight.DepartureDateTime,
                ArrivalDateTime = flight.ArrivalDateTime,
                FlightNumber = flight.FlightNumber,
                Status = SetStatus(flight)
            }).ToList();

            return flights.OrderBy(item => item.DepartureDateTime).ThenBy(item => item.FlightNumber).ToList();
        }

        private StatusResponse.FlightStatus SetStatus(FlightStatus flight)
        {
            var now = DateTime.Now;

            // Check if flight has landed
            if (flight.ArrivalDateTime <= now)
                return StatusResponse.FlightStatus.Landed;

            // Check if flight has departed but not yet arrived
            if (flight.DepartureDateTime <= now && flight.ArrivalDateTime > now)
                return StatusResponse.FlightStatus.Departed;

            // Check if within 60 minutes of departure
            if (now >= flight.DepartureDateTime.AddHours(-1) && now < flight.DepartureDateTime)
                return StatusResponse.FlightStatus.Boarding;

            // Otherwise, flight is on time and not yet boarding
            return StatusResponse.FlightStatus.OnTime;
        }
    }
}
