using System.Text.Json;
using Microsoft.Extensions.Logging;
using Res.Microservices.Inventory.Application.DTOs;
using Res.Microservices.Inventory.Application.Importer;
using Res.Microservices.Inventory.Application.Interfaces;
using Res.Microservices.Inventory.Domain.Entities;
using Res.Microservices.Inventory.Infrastructure.Models;
using Res.Microservices.Inventory.Infrastructure.Repositories;

namespace Res.Microservices.Inventory.Application.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IFlightRepository _flightRepository;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(IFlightRepository flightRepository, ILogger<InventoryService> logger)
        {
            _flightRepository = flightRepository;
            _logger = logger;
        }

        public async Task<List<FlightSearchResponse>> SearchFlights(FlightSearchRequest request)
        {
            try
            {
                var flights = await _flightRepository.SearchFlights(request.DepartureDate, request.Origin, request.Destination);

                return flights.Select(flight =>
                {
                    var arrivalOffset = (flight.Arrival.Date - flight.Departure.Date).Days;

                    return new FlightSearchResponse
                    {
                        FlightNo = flight.FlightNo,
                        Origin = flight.From,
                        Destination = flight.To,
                        Departure = flight.Departure,
                        Arrival = flight.Arrival,
                        ArrivalOffset = arrivalOffset,
                        Aircraft = flight.AircraftType,
                        Availability = flight.Seats.Select(cs => new FlightSearchResponse.CabinAvailability
                        {
                            Cabin = cs.Key,
                            Remaining = cs.Value
                        }).ToList()
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching flights for {Date} {Origin}-{Destination}",
                    request.DepartureDate, request.Origin, request.Destination);
                throw;
            }
        }

        public async Task ImportSchedule(ImportScheduleRequest request)
        {
            try
            {
                //if (request.StartDate > request.EndDate)
                //{
                //    throw new ArgumentException("Start date must be before or equal to end date");
                //}

                DataImporter dataImporter = new DataImporter();

                var aircraftConfigs = dataImporter.LoadAircraftConfigs();
                var flights = dataImporter.LoadFlights(aircraftConfigs);

                //await _flightRepository.ImportAircraftConfig(aircraftConfigs);
                await _flightRepository.ImportFlights(flights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing flight schedule from {StartDate} to {EndDate}",
                    request.StartDate, request.EndDate);
                throw;
            }
        }

        public async Task<List<AllocationResponse>> GetSeatMap(AllocationRequest request)
        {
            try
            {
                return await _flightRepository.GetSeatMap(request.FlightNo, request.DepartureDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving seat map for flight {FlightNo} on {Date}",
                    request.FlightNo, request.DepartureDate);
                throw;
            }
        }

        public async Task<bool> AllocateSeat(SeatAllocationRequest request)
        {
            try
            {
                return await _flightRepository.AllocateSeat(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating seat {Seat} on {FlightNo} for {RecordLocator}",
                    request.SeatNumber, request.FlightNo, request.RecordLocator);
                throw;
            }
        }
    }
}