using Microsoft.Extensions.Logging;
using Res.Api.Common.Helpers;
using Res.Api.Models;
using Res.Core.Interfaces;
using Res.Infrastructure.Interfaces;

namespace Res.Api.Core.Services
{
    public class SeatService : ISeatService
    {
        private readonly IInventoryService _inventoryService;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ISeatMapRepository _seatMapRepository;
        private readonly ILogger<SeatService> _logger;

        public SeatService(
            IInventoryService inventoryService,
            ISeatMapRepository seatMapRepository,
            IInventoryRepository inventoryRepository,
            ILogger<SeatService> logger)
        {
            _inventoryService = inventoryService;
            _seatMapRepository = seatMapRepository;
            _inventoryRepository = inventoryRepository;
            _logger = logger;
        }

        public async Task<SeatMapResponse> ViewSeatMap(SeatMapRequest request)
        {
            try
            {
                // Convert DateTime to airline format
                string airlineDate = request.DepartureDate.ToAirlineFormat();

                // 1. Get flight details from inventory
                var flight = await _inventoryService.FindFlight(request.FlightNumber, airlineDate);
                if (flight == null)
                {
                    _logger.LogWarning("Flight {FlightNumber} on {DepartureDate} not found",
                        request.FlightNumber, airlineDate);
                    return new SeatMapResponse { Success = false, Message = "Flight not found" };
                }

                // 2. Get seat configuration for aircraft type
                if (!_seatMapRepository.SeatConfigurations.TryGetValue(flight.AircraftType, out var seatConfig))
                {
                    _logger.LogWarning("No seat configuration found for aircraft type {AircraftType}",
                        flight.AircraftType);
                    return new SeatMapResponse { Success = false, Message = "Seat configuration not found" };
                }

                // 3. Get occupied seats for this flight
                var key = $"{request.FlightNumber}-{airlineDate}";
                var occupiedSeats = new HashSet<string>();
                if (_inventoryRepository.SeatInventory.TryGetValue(key, out var seatInventory))
                {
                    occupiedSeats = seatInventory.OccupiedSeats;
                }

                // 4. Build seatmap response
                var seatMap = new SeatMapResponse
                {
                    Success = true,
                    FlightNumber = request.FlightNumber,
                    DepartureDate = request.DepartureDate,
                    AircraftType = flight.AircraftType,
                    Cabins = new List<SeatMapResponse.CabinInfo>()
                };

                // 5. Process each cabin
                foreach (var cabin in seatConfig.Cabins)
                {
                    var cabinInfo = new SeatMapResponse.CabinInfo
                    {
                        CabinCode = cabin.Value.CabinCode,
                        CabinName = cabin.Value.CabinName,
                        Rows = new List<SeatMapResponse.RowInfo>()
                    };

                    // Process each row in the cabin
                    for (int rowNum = cabin.Value.FirstRow; rowNum <= cabin.Value.LastRow; rowNum++)
                    {
                        var rowInfo = new SeatMapResponse.RowInfo
                        {
                            RowNumber = rowNum,
                            Seats = new List<SeatMapResponse.SeatInfo>()
                        };

                        // Process each seat in the row
                        foreach (var seatLetter in cabin.Value.SeatLetters)
                        {
                            var seatNumber = $"{rowNum}{seatLetter}";
                            var seatDefinition = cabin.Value.SeatDefinitions[seatLetter];
                            var blockedSeat = cabin.Value.BlockedSeats
                                .FirstOrDefault(bs => bs.SeatNumber == seatNumber);

                            var seatInfo = new SeatMapResponse.SeatInfo
                            {
                                SeatNumber = seatNumber,
                                IsAvailable = !occupiedSeats.Contains(seatNumber) && blockedSeat == null,
                                IsExit = cabin.Value.ExitRows.Contains(rowNum),
                                IsBulkhead = cabin.Value.BulkheadRows.Contains(rowNum),
                                IsAisle = seatDefinition.IsAisle,
                                IsWindow = seatDefinition.IsWindow,
                                BlockedReason = blockedSeat?.Reason
                            };

                            rowInfo.Seats.Add(seatInfo);
                        }

                        cabinInfo.Rows.Add(rowInfo);
                    }

                    seatMap.Cabins.Add(cabinInfo);
                }

                return seatMap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating seatmap for flight {FlightNumber} on {DepartureDate}",
                    request.FlightNumber, request.DepartureDate);

                return new SeatMapResponse
                {
                    Success = false,
                    Message = "Internal server error generating seatmap"
                };
            }
        }
    }
}