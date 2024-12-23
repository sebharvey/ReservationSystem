using Res.Core.Interfaces;
using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.Pnr;
using Res.Domain.Entities.SeatMap;
using Res.Infrastructure.Interfaces;

namespace Res.Core.Services
{
    public class SeatService : ISeatService
    {
        public Pnr Pnr { get; set; }

        private readonly IInventoryService _inventoryService;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ISeatMapRepository _seatMapRepository;
        
        public SeatService(IInventoryService inventoryService, ISeatMapRepository seatMapRepository, IInventoryRepository inventoryRepository)
        {
            _inventoryService = inventoryService;
            _seatMapRepository = seatMapRepository;
            _inventoryRepository = inventoryRepository;
        }

        public async Task<SeatMap> DisplaySeatMap(string flightNumber, string departureDate, string bookingClass = null)
        {
            var flight = await _inventoryService.FindFlight(flightNumber, departureDate);

            if (flight == null)
                return null;

            var seatConfig = _seatMapRepository.SeatConfigurations[flight.AircraftType];

            // Get occupied seats from inventory
            var flightInventory = GetFlightSeatInventory(flightNumber, departureDate);
            var occupiedSeats = flightInventory?.OccupiedSeats ?? new HashSet<string>();

            var seatMap = new SeatMap
            {
                FlightNumber = flightNumber,
                DepartureDate = departureDate,
                AircraftType = flight.AircraftType
            };

            foreach (var cabin in seatConfig.Cabins)
            {
                // Skip cabins that don't match the booking class when using SM1
                if (bookingClass != null && cabin.Value.CabinCode != bookingClass)
                    continue;

                var cabinMap = new CabinMap
                {
                    CabinCode = cabin.Value.CabinCode,
                    CabinName = cabin.Value.CabinName
                };

                for (int rowNum = cabin.Value.FirstRow; rowNum <= cabin.Value.LastRow; rowNum++)
                {
                    var row = new SeatRow { RowNumber = rowNum };

                    foreach (var letter in cabin.Value.SeatLetters)
                    {
                        var seatDef = cabin.Value.SeatDefinitions[letter];
                        var seatNumber = $"{rowNum}{letter}";

                        var blockedSeat = cabin.Value.BlockedSeats.FirstOrDefault(bs => bs.SeatNumber == seatNumber);

                        var seat = new Seat
                        {
                            SeatNumber = seatNumber,
                            IsExit = cabin.Value.ExitRows.Contains(rowNum),
                            IsBulkhead = cabin.Value.BulkheadRows.Contains(rowNum),
                            IsAisle = seatDef.IsAisle,
                            IsWindow = seatDef.IsWindow
                        };

                        // Check if seat is already occupied in inventory
                        if (occupiedSeats.Contains(seatNumber))
                        {
                            seat.Status = "X";

                            // If this is a PNR display, check if it's assigned to current PNR
                            if (Pnr.Data != null)
                            {
                                var assignment = Pnr.Data.SeatAssignments.FirstOrDefault(sa =>
                                    sa.SeatNumber == seatNumber);

                                if (assignment != null)
                                {
                                    var passenger = Pnr.Data.Passengers.First(p => p.PassengerId == assignment.PassengerId);
                                    seat.BlockedReason = $"Assigned to {passenger.LastName}/{passenger.FirstName}";
                                }
                                else
                                {
                                    seat.BlockedReason = "Occupied";
                                }
                            }
                        }
                        else if (blockedSeat != null)
                        {
                            seat.Status = "B";
                            seat.BlockedReason = blockedSeat.Reason;
                        }
                        else
                        {
                            seat.Status = "A";
                        }

                        row.Seats.Add(seat);
                    }

                    cabinMap.Rows.Add(row);
                }

                seatMap.Cabins.Add(cabinMap);
            }

            return seatMap;
        }

        private FlightSeatInventory GetFlightSeatInventory(string flightNumber, string departureDate)
        {
            var key = $"{flightNumber}-{departureDate}";
            return _inventoryRepository.SeatInventory.TryGetValue(key, out var inventory) ? inventory : null;
        }

        public async Task<bool> AssignSeat(string seatNumber, int passengerId, string segmentNumber)
        {
            // Validate passenger exists
            var passenger = Pnr.Data.Passengers.FirstOrDefault(p => p.PassengerId == passengerId);
            if (passenger == null)
                throw new ArgumentException("Invalid passenger number");

            // Get segment
            if (int.Parse(segmentNumber) > Pnr.Data.Segments.Count)
                throw new ArgumentException("Invalid segment number");

            var segment = Pnr.Data.Segments[int.Parse(segmentNumber) - 1];

            // Get flight details including aircraft type from inventory
            var flight = await _inventoryService.FindFlight(segment.FlightNumber, segment.DepartureDate);
            if (flight == null)
                throw new InvalidOperationException("Flight not found");

            // Validate seat exists and is available
            if (!await _inventoryService.IsValidSeat(segment.FlightNumber, segment.DepartureDate, seatNumber))
                throw new InvalidOperationException("Invalid seat number");

            // Get cabin config for seat
            var cabin = await _inventoryService.GetCabinConfigForSeat(flight.AircraftType, seatNumber);

            // Validate booking class matches cabin
            if (cabin.CabinCode != segment.BookingClass)
                throw new InvalidOperationException("Invalid cabin for booking class");

            // Check if seat is available
            if (!await _inventoryService.IsSeatAvailable(segment.FlightNumber, segment.DepartureDate, seatNumber))
                throw new InvalidOperationException("Seat not available");

            // Remove any existing seat assignment
            var existingSeat = Pnr.Data.SeatAssignments.FirstOrDefault(s =>
                s.PassengerId == passengerId && s.SegmentNumber == segmentNumber);

            if (existingSeat != null)
            {
                await _inventoryService.ReleaseSeat(segment.FlightNumber, segment.DepartureDate, existingSeat.SeatNumber);
                Pnr.Data.SeatAssignments.Remove(existingSeat);
            }

            // Assign new seat
            if (!await _inventoryService.AssignSeat(segment.FlightNumber, segment.DepartureDate, seatNumber))
                throw new InvalidOperationException("Unable to assign seat");

            // Add seat assignment to PNR
            Pnr.Data.SeatAssignments.Add(new SeatAssignment
            {
                PassengerId = passengerId,
                SegmentNumber = segmentNumber,
                SeatNumber = seatNumber,
                AssignedDate = DateTime.UtcNow
            });

            return true;
        }

        public async Task<bool> RemoveSeat(int passengerId, string segmentNumber)
        {
            var seatAssignment = Pnr.Data.SeatAssignments.FirstOrDefault(s =>
                s.PassengerId == passengerId && s.SegmentNumber == segmentNumber);

            if (seatAssignment == null)
                throw new InvalidOperationException("No seat assignment found");

            var segment = Pnr.Data.Segments[int.Parse(segmentNumber) - 1];

            // Release seat in inventory
            await _inventoryService.ReleaseSeat(segment.FlightNumber, segment.DepartureDate, seatAssignment.SeatNumber);

            // Remove from PNR
            Pnr.Data.SeatAssignments.Remove(seatAssignment);

            return true;
        }
    }
}
