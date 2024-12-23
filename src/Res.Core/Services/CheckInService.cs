using System.Globalization;
using Res.Core.Common.Helpers;
using Res.Core.Interfaces;
using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.Pnr;
using Res.Domain.Entities.Ticket;
using Res.Domain.Enums;
using Res.Domain.Requests;
using Res.Infrastructure.Interfaces;

namespace Res.Core.Services
{
    public class CheckInService : ICheckInService
    {
        private readonly IReservationService _reservationService;
        private readonly IApisService _apisService;
        private readonly IBoardingPassService _boardingPassService;
        private readonly IInventoryService _inventoryService;
        private readonly ISeatMapRepository _seatMapRepository;
        private readonly IInventoryRepository _inventoryRepository;

        private readonly Dictionary<string, HashSet<string>> _allocatedSeats = new();

        public CheckInService(IReservationService reservationService, IApisService apisService, IBoardingPassService boardingPassService, IInventoryService inventoryService, ISeatMapRepository seatMapRepository, IInventoryRepository inventoryRepository)
        {
            _reservationService = reservationService;
            _apisService = apisService;
            _boardingPassService = boardingPassService;
            _inventoryService = inventoryService;
            _seatMapRepository = seatMapRepository;
            _inventoryRepository = inventoryRepository;
        }

        public async Task<BoardingPass> CheckIn(string recordLocator, CheckInRequest request)
        {
            // 1. Retrieve PNR and perform initial validations
            await _reservationService.RetrievePnr(recordLocator);

            if (_reservationService.Pnr == null)
                throw new Exception("PNR NOT FOUND");

            // 2. Validate passenger exists
            var passenger = _reservationService.Pnr.Data.Passengers.FirstOrDefault(p => p.PassengerId == request.PassengerId);
            if (passenger == null)
                throw new Exception("PASSENGER NOT FOUND");

            // 3. Find matching flight segment
            var segment = _reservationService.Pnr.Data.Segments.FirstOrDefault(s => s.FlightNumber == request.FlightNumber);
            if (segment == null)
                throw new Exception("SEGMENT NOT FOUND");

            // 4. Find and validate ticket
            var ticket = _reservationService.Pnr.Data.Tickets.FirstOrDefault(t => t.PassengerId == request.PassengerId);
            if (ticket == null)
                throw new Exception("TICKET NOT FOUND");

            var coupon = ticket.Coupons.FirstOrDefault(c =>
                c.FlightNumber == request.FlightNumber &&
                c.Status == CouponStatus.Open);

            if (coupon == null)
                throw new Exception("NO VALID COUPON FOUND");

            // 5-8. Perform various validations...
            await ValidateCheckinWindow(segment);
            await ValidateTravelDocuments(_reservationService.Pnr.Data.SpecialServiceRequests.Where(s => s.Code == "DOCS").ToList(), segment);
            await PerformSecurityChecks(_reservationService.Pnr, passenger, request);
            await SubmitApisInformation(recordLocator, request, segment);

            // 9. Check and assign seat
            string finalSeatNumber = request.SeatNumber;

            // If no seat specified, try to assign one
            if (string.IsNullOrEmpty(finalSeatNumber))
            {
                // First check for pre-assigned seat
                var existingSeat = _reservationService.Pnr.Data.SeatAssignments.FirstOrDefault(s =>
                    s.PassengerId == request.PassengerId &&
                    s.SegmentNumber == (_reservationService.Pnr.Data.Segments.IndexOf(segment) + 1).ToString());

                if (existingSeat != null)
                {
                    finalSeatNumber = existingSeat.SeatNumber;
                }
                else
                {
                    // No pre-assigned seat, get a random one based on booking class
                    finalSeatNumber = await AssignRandomSeat(segment.FlightNumber, segment.DepartureDate, segment.BookingClass);

                    if (finalSeatNumber == null)
                        throw new Exception("NO SEATS AVAILABLE");

                    // Add seat assignment to PNR
                    _reservationService.Pnr.Data.SeatAssignments.Add(new SeatAssignment
                    {
                        PassengerId = request.PassengerId,
                        SegmentNumber = (_reservationService.Pnr.Data.Segments.IndexOf(segment) + 1).ToString(),
                        SeatNumber = finalSeatNumber,
                        AssignedDate = DateTime.UtcNow
                    });
                }
            }
            else
            {
                // Validate specified seat
                await ValidateSeat(segment.FlightNumber, finalSeatNumber, ticket, _reservationService.Pnr, request.PassengerId);
            }

            // 10. Create boarding pass with assigned seat

            _boardingPassService.Pnr = _reservationService.Pnr;

            var boardingPass = _boardingPassService.GenerateBoardingPass(new BoardingPassRequest
            {
                RecordLocator = recordLocator,
                PassengerId = request.PassengerId,
                TicketNumber = ticket.TicketNumber,
                FlightNumber = segment.FlightNumber,
                Origin = segment.Origin,
                Destination = segment.Destination,
                DepartureDate = segment.DepartureDate,
                DepartureTime = segment.DepartureTime,
                SeatNumber = finalSeatNumber,
                HasCheckedBags = request.HasCheckedBags,
                BaggageWeight = request.BaggageWeight,
                BaggageCount = request.BaggageCount,
                SsrCodes = request.SsrCodes
            });

            // Store passenger reference for display
            boardingPass.Passenger = passenger;

            // Add special handling instructions
            AddSpecialHandlingInstructions(boardingPass, _reservationService.Pnr.Data.SpecialServiceRequests, request.SsrCodes);

            // Update ticket coupon status
            coupon.Status = CouponStatus.CheckedIn;

            // Add check-in OSIs to PNR
            AddCheckInOsis(_reservationService.Pnr, boardingPass);

            // Save changes to PNR
            await _reservationService.CommitPnr();

            return boardingPass;
        }

        private string GetAssignedSeat(string flightNumber, int passengerId)
        {
            if (!_allocatedSeats.ContainsKey(flightNumber))
                return null;

            var allocation = _allocatedSeats[flightNumber]
                .FirstOrDefault(s => s.StartsWith($"{passengerId}-"));

            return allocation?.Split('-')[1];
        }

        private async Task SubmitApisInformation(string recordLocator, CheckInRequest request, Segment segment)
        {
            if (IsInternationalFlight(segment))
            {
                // Build APIS data from PNR
                var apisData = await _apisService.BuildApisFromPnr(recordLocator, request.FlightNumber);

                // Validate APIS data
                if (!await _apisService.ValidateApisData(apisData))
                    throw new Exception("INVALID APIS DATA");

                // Submit APIS data
                if (!await _apisService.SubmitApisData(apisData))
                    throw new Exception("APIS SUBMISSION FAILED");
            }
        }

        public async Task<List<BoardingPass>> CheckInAll(string recordLocator, string flightNumber)
        {
            // 1. Retrieve PNR
            await _reservationService.RetrievePnr(recordLocator);

            if (_reservationService.Pnr == null)
                throw new Exception("PNR NOT FOUND");

            // Find matching flight segment
            var segment = _reservationService.Pnr.Data.Segments.FirstOrDefault(s => !s.IsSurfaceSegment && s.FlightNumber == flightNumber);
            if (segment == null)
                throw new Exception("FLIGHT SEGMENT NOT FOUND");

            var boardingPasses = new List<BoardingPass>();
            var errors = new List<string>();

            // Check in each passenger
            foreach (var passenger in _reservationService.Pnr.Data.Passengers)
            {
                try
                {
                    // Auto-assign seat based on class of service
                    var ticket = _reservationService.Pnr.Data.Tickets.FirstOrDefault(t => t.PassengerId == passenger.PassengerId);
                    if (ticket == null)
                        throw new Exception($"NO TICKET FOUND FOR PASSENGER {passenger.PassengerId}");

                    var coupon = ticket.Coupons.FirstOrDefault(c =>
                        c.FlightNumber == flightNumber &&
                        c.Status == CouponStatus.Open);

                    if (coupon == null)
                        throw new Exception($"NO VALID COUPON FOUND FOR PASSENGER {passenger.PassengerId}");

                    var request = new CheckInRequest
                    {
                        RecordLocator = recordLocator,
                        PassengerId = passenger.PassengerId,
                        FlightNumber = flightNumber,
                        HasCheckedBags = false, // Default to no bags for auto check-in
                    };

                    var boardingPass = await CheckIn(recordLocator, request);
                    boardingPasses.Add(boardingPass);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to check in passenger {passenger.PassengerId}: {ex.Message}");
                }
            }

            if (errors.Any())
                throw new AggregateException("Some passengers could not be checked in:", errors.Select(e => new Exception(e)));

            return boardingPasses;
        }

        private bool IsInternationalFlight(Segment segment)
        {
            // Simple example - you'd want a more comprehensive country code list
            var domesticAirports = new[] { "LHR", "LGW", "MAN", "BHX", "EDI", "GLA" };

            return !domesticAirports.Contains(segment.Origin) ||
                   !domesticAirports.Contains(segment.Destination);
        }

        private async Task<string> AssignRandomSeat(string flightNumber, string departureDate, string bookingClass)
        {
            // Get flight details to determine aircraft type
            var flight = await _inventoryService.FindFlight(flightNumber, departureDate);
            if (flight == null) return null;

            // Get seat configuration for this aircraft type
            var seatConfig = _seatMapRepository.SeatConfigurations[flight.AircraftType];

            // Find the cabin matching the booking class
            var matchingCabins = seatConfig.Cabins.Where(c => c.Value.CabinCode == bookingClass).ToList();
            if (!matchingCabins.Any()) return null;

            var cabin = matchingCabins.First().Value;

            // Get currently allocated seats for this flight
            var key = $"{flightNumber}-{departureDate}";
            var flightInventory = _inventoryRepository.SeatInventory.GetValueOrDefault(key);
            var occupiedSeats = flightInventory?.OccupiedSeats ?? new HashSet<string>();

            // Build list of all possible seats for this cabin
            var availableSeats = new List<SeatAssignmentCandidate>();

            for (int row = cabin.FirstRow; row <= cabin.LastRow; row++)
            {
                foreach (var letter in cabin.SeatLetters)
                {
                    var seatNumber = $"{row}{letter}";

                    // Skip if seat is blocked or already occupied
                    if (cabin.BlockedSeats.Any(bs => bs.SeatNumber == seatNumber) ||
                        occupiedSeats.Contains(seatNumber))
                    {
                        continue;
                    }

                    var seatDef = cabin.SeatDefinitions[letter];
                    var score = CalculateSeatScore(seatDef, row, cabin);

                    availableSeats.Add(new SeatAssignmentCandidate
                    {
                        SeatNumber = seatNumber,
                        Score = score
                    });
                }
            }

            // No seats available
            if (!availableSeats.Any()) return null;

            // Sort seats by score (higher is better) and add some randomization
            var random = new Random();
            var topSeats = availableSeats
                .OrderByDescending(s => s.Score)
                .Take(5) // Take top 5 seats
                .ToList();

            // Randomly select from top seats
            var selectedSeat = topSeats[random.Next(topSeats.Count)];

            // Attempt to assign the seat
            if (await _inventoryService.AssignSeat(flightNumber, departureDate, selectedSeat.SeatNumber))
            {
                return selectedSeat.SeatNumber;
            }

            return null;
        }

        private class SeatAssignmentCandidate
        {
            public string SeatNumber { get; set; }
            public int Score { get; set; }
        }

        private int CalculateSeatScore(BaseSeatDefinition seatDef, int row, CabinConfiguration cabin)
        {
            var score = 0;

            // Prefer window and aisle seats
            if (seatDef.IsWindow) score += 3;
            if (seatDef.IsAisle) score += 2;

            // Avoid middle seats
            if (seatDef.IsMiddle) score -= 2;

            // Prefer seats in the middle of the cabin (not too front or back)
            var cabinMiddle = (cabin.FirstRow + cabin.LastRow) / 2;
            var distanceFromMiddle = Math.Abs(row - cabinMiddle);
            score -= distanceFromMiddle;

            // Slightly prefer forward cabin positions
            score += cabin.LastRow - row;

            // Extra points for exit row seats
            if (cabin.ExitRows.Contains(row)) score += 2;

            // Slight penalty for bulkhead due to reduced legroom
            if (cabin.BulkheadRows.Contains(row)) score -= 1;

            // Penalty for seats near galleys
            if (cabin.GalleryRows.Contains(row)) score -= 2;

            return score;
        }

        public async Task<bool> ValidateCheckinWindow(Segment segment)
        {
            var departureTime = DateTimeHelper.CombineDateAndTime(segment.DepartureDate, segment.DepartureTime);

            if (DateTime.Now < departureTime.AddHours(-24))
                throw new Exception("CHECK-IN NOT YET OPEN");

            if (DateTime.Now > departureTime.AddMinutes(-45))
                throw new Exception("CHECK-IN CLOSED");

            return true;
        }

        public async Task<Pnr> ValidatePnr(string requestRecordLocator, string requestFrom)
        {
            // 1. Retrieve PNR and validate it exists
            await _reservationService.RetrievePnr(requestRecordLocator);

            if (_reservationService.Pnr == null)
                throw new InvalidOperationException("PNR NOT FOUND");

            // 2. Find matching flight segment
            var segment = _reservationService.Pnr.Data.Segments.FirstOrDefault(s => s.Origin == requestFrom);
            if (segment == null)
                throw new InvalidOperationException("SEGMENT NOT FOUND");

            // 3. Validate PNR is ticketed
            if (_reservationService.Pnr.Data.Status != PnrStatus.Ticketed)
                throw new InvalidOperationException("PNR NOT VALID");

            // 4. Validate check-in window
            bool isWithinWindow = await ValidateCheckinWindow(segment);
            if (!isWithinWindow)
                throw new InvalidOperationException("NOT IN CHECKIN WINDOW");

            // 5. Verify no passengers are already checked in for this segment
            foreach (var passenger in _reservationService.Pnr.Data.Passengers)
            {
                var ticket = _reservationService.Pnr.Data.Tickets.FirstOrDefault(t => t.PassengerId == passenger.PassengerId);
                if (ticket == null)
                    continue;

                var coupon = ticket.Coupons.FirstOrDefault(c => c.FlightNumber == segment.FlightNumber && c.DepartureDate == segment.DepartureDate);

                if (coupon == null)
                    continue;

                // Check if the coupon status indicates already checked in
                if (coupon.Status == CouponStatus.CheckedIn || coupon.Status == CouponStatus.Lifted || coupon.Status == CouponStatus.Used)
                {
                    throw new InvalidOperationException($"PASSENGER {passenger.PassengerId} ({passenger.LastName}/{passenger.FirstName}) ALREADY CHECKED IN");
                }
            }

            return _reservationService.Pnr;
        }

        private async Task ValidateTravelDocuments(List<Ssr> documents, Segment segment)
        {
            if (!documents.Any())
                throw new Exception("TRAVEL DOCUMENTS REQUIRED");

            foreach (var doc in documents)
            {
                string[] parts = doc.Text.Split('/');

                Document document = new Document
                {
                    StatusCode = parts[0], // HK1
                    Type = parts[1], // P
                    IssuingCountry = parts[2], // GBR
                    Number = parts[3], // P12345678
                    Nationality = parts[4], // GBR
                    Gender = parts[6], // M  
                    Surname = parts[8], // LASTNAME
                    Firstname = parts[9], // FIRSTNAME
                    ExpiryDate = DateTimeHelper.ParseAirlineLongDateAndYear(parts[7]),
                    DateOfBirth = DateTimeHelper.ParseAirlineLongDateAndYear(parts[5])
                };

                // Basic validation
                if (document.ExpiryDate <= DateTime.Now)
                    throw new Exception($"EXPIRED DOCUMENT: {document.Type} {document.Number}");

                if (document.Type == "P" && (string.IsNullOrEmpty(document.Number) ||
                                             string.IsNullOrEmpty(document.IssuingCountry) ||
                                             string.IsNullOrEmpty(document.Nationality)))
                    throw new Exception("INCOMPLETE PASSPORT DETAILS");

                // Route specific validation
                await ValidateRouteDocumentRequirements(document, segment);
            }
        }

        private async Task ValidateRouteDocumentRequirements(Document doc, Segment segment)
        {
            // Validate passport expiry requirements
            var minExpiryRequired = DateTime.Now.AddMonths(6);

            if (doc.ExpiryDate < minExpiryRequired)
                throw new Exception("PASSPORT MUST BE VALID FOR AT LEAST 6 MONTHS");
        }

        private async Task PerformSecurityChecks(Pnr pnr, Passenger passenger, CheckInRequest request)
        {
            // Placeholder for security check logic
            // Here we would typically:
            // 1. Check passenger against No Fly lists
            // 2. Verify passenger isn't on any watch lists
            // 3. Check for any travel restrictions
            // 4. Validate any security questions
            // 5. Check baggage rules compliance
        }

        private async Task ValidateSeat(string flightNumber, string seatNumber, Ticket ticket, Pnr pnr, int passengerId)
        {
            // Find segment index
            var segmentIndex = pnr.Data.Segments.FindIndex(s => s.FlightNumber == flightNumber) + 1;
            string finalSeatNumber = seatNumber;

            // If no seat specified, first check if there's a pre-assigned seat
            if (string.IsNullOrEmpty(seatNumber))
            {
                var existingSeat = pnr.Data.SeatAssignments.FirstOrDefault(s => s.PassengerId == passengerId &&
                                                                           s.SegmentNumber == segmentIndex.ToString());

                if (existingSeat != null)
                {
                    finalSeatNumber = existingSeat.SeatNumber;
                }
                else
                {
                    // No pre-assigned seat, get a random one
                    var segment = pnr.Data.Segments[segmentIndex - 1];
                    finalSeatNumber = await AssignRandomSeat(segment.FlightNumber, segment.DepartureDate, segment.BookingClass);

                    if (finalSeatNumber == null)
                        throw new Exception("NO SEATS AVAILABLE");

                    // Add seat assignment to PNR
                    pnr.Data.SeatAssignments.Add(new SeatAssignment
                    {
                        PassengerId = passengerId,
                        SegmentNumber = segmentIndex.ToString(),
                        SeatNumber = finalSeatNumber,
                        AssignedDate = DateTime.UtcNow
                    });
                }
            }

            // Validate requested seat
            if (!await _inventoryService.IsValidSeat(flightNumber, pnr.Data.Segments[segmentIndex - 1].DepartureDate, finalSeatNumber))
                throw new Exception("INVALID SEAT NUMBER");

            if (!await _inventoryService.IsSeatAvailable(flightNumber, pnr.Data.Segments[segmentIndex - 1].DepartureDate, finalSeatNumber))
                throw new Exception("SEAT NOT AVAILABLE");

            if (!await _inventoryService.AssignSeat(flightNumber, pnr.Data.Segments[segmentIndex - 1].DepartureDate, finalSeatNumber))
                throw new Exception("UNABLE TO ASSIGN SEAT");

            if (!_allocatedSeats.ContainsKey(flightNumber))
                _allocatedSeats[flightNumber] = new HashSet<string>();

            _allocatedSeats[flightNumber].Add($"{passengerId}-{finalSeatNumber}");
        }

        private async void AddSpecialHandlingInstructions(BoardingPass boardingPass, List<Ssr> ssrs, List<string> ssrCodes)
        {
            foreach (var ssr in ssrs.Where(s => s.PassengerId == boardingPass.PassengerId))
            {
                switch (ssr.Type)
                {
                    case SsrType.Wheelchair:
                        boardingPass.SecurityMessages.Add("WHEELCHAIR ASSISTANCE REQUIRED");
                        break;
                    case SsrType.MealPreference:
                        boardingPass.SecurityMessages.Add($"SPECIAL MEAL: {ssr.Code}");
                        break;
                        // Add more special handling cases
                }
            }

            // Handle any additional SSR codes provided during check-in
            foreach (var ssrCode in ssrCodes)
            {
                boardingPass.SecurityMessages.Add($"CHECK-IN SSR: {ssrCode}");
            }

            await _reservationService.CommitPnr();
        }

        private async void AddCheckInOsis(Pnr pnr, BoardingPass boardingPass)
        {
            pnr.Data.OtherServiceInformation.Add(new Osi
            {
                PnrLocator = pnr.RecordLocator,
                Category = OsiCategory.OperationalInfo,
                CompanyId = boardingPass.FlightNumber.Substring(0, 2),
                Text = $"CKIN {boardingPass.CheckInTime.ToString("ddMMMyy", CultureInfo.InvariantCulture).ToUpper()} {boardingPass.CheckInTime:HHmm} {boardingPass.SeatNumber}",
                CreatedDate = DateTime.UtcNow
            });

            if (boardingPass.HasCheckedBags)
            {
                pnr.Data.OtherServiceInformation.Add(new Osi
                {
                    PnrLocator = pnr.RecordLocator,
                    Category = OsiCategory.OperationalInfo,
                    CompanyId = boardingPass.FlightNumber.Substring(0, 2),
                    Text = $"BAGS {boardingPass.BaggageCount}/{boardingPass.BaggageWeight}KG",
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _reservationService.CommitPnr();
        }

        public async Task<bool> CancelCheckIn(string recordLocator, int passengerId, string flightNumber)
        {
            await _reservationService.RetrievePnr(recordLocator);

            if (_reservationService.Pnr == null)
                throw new Exception("PNR NOT FOUND");

            var ticket = _reservationService.Pnr.Data.Tickets.FirstOrDefault(t => t.PassengerId == passengerId);
            if (ticket == null)
                throw new Exception("TICKET NOT FOUND");

            var coupon = ticket.Coupons.FirstOrDefault(c =>
                c.FlightNumber == flightNumber &&
                c.Status == CouponStatus.Used);

            if (coupon == null)
                throw new Exception("NO CHECKED-IN COUPON FOUND");

            // Reset coupon status
            coupon.Status = CouponStatus.Open;

            // Remove seat allocation
            if (_allocatedSeats.ContainsKey(flightNumber))
            {
                _allocatedSeats[flightNumber].RemoveWhere(s => s.StartsWith($"{passengerId}-"));
            }

            // Add cancellation OSI
            _reservationService.Pnr.Data.OtherServiceInformation.Add(new Osi
            {
                PnrLocator = _reservationService.Pnr.RecordLocator,
                Category = OsiCategory.OperationalInfo,
                CompanyId = flightNumber.Substring(0, 2),
                Text = $"CXLD CKIN {DateTime.UtcNow:ddMMMyy/HHmm}",
                CreatedDate = DateTime.UtcNow
            });

            await _reservationService.CommitPnr();

            return true;
        }
    }
}