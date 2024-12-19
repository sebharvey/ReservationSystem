using Res.Core.Common.Helpers;
using Res.Core.Interfaces;
using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;
using Res.Domain.Requests;
using Res.Domain.Responses;
using Res.Infrastructure.Interfaces;

namespace Res.Core.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IPnrRepository _pnrRepository;
        private readonly IInventoryService _inventoryService;

        public ReservationService(IPnrRepository pnrRepository, IInventoryService inventoryService)
        {
            _pnrRepository = pnrRepository;
            _inventoryService = inventoryService;
        }

        public async Task<Pnr> CreatePnrWorkspace(UserContext user)
        {
            return await Task.FromResult(new Pnr
            {
                Status = PnrStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                Passengers = new List<Passenger>(),
                Segments = new List<Segment>(),
                Agency = new AgencyInfo { AgencyCode = "VS", AgentId = user.UserId, OfficeId = "LON1A2BC3" },
                Contact = new ContactInfo(),
                Remarks = new List<string>(),
                TicketingInfo = new TicketingInfo(),
                SpecialServiceRequests = new List<Ssr>(),
                OtherServiceInformation = new List<Osi>()
            });
        }

        public async Task<Pnr> AddName(Pnr pnr, string lastName, string firstName, string title, PassengerType type)
        {
            if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("Last name and first name are required");

            var passenger = new Passenger
            {
                PassengerId = pnr.Passengers.Count + 1,
                LastName = lastName.ToUpper(),
                FirstName = firstName.ToUpper(),
                Title = title?.ToUpper() ?? string.Empty,
                Type = type,
                Documents = new List<Document>()
            };

            pnr.Passengers.Add(passenger);

            return await Task.FromResult(pnr);
        }

        public async Task<Pnr> AddSegment(Pnr pnr, SellSegmentRequest sellSegmentRequest)
        {
            // Check and decrement inventory before adding segment
            if (!_inventoryService.DecrementInventory(sellSegmentRequest.FlightNumber, sellSegmentRequest.DepartureDate, sellSegmentRequest.BookingClass, sellSegmentRequest.Quantity))
                throw new InvalidOperationException($"Not enough {sellSegmentRequest.BookingClass} class seats available on {sellSegmentRequest.FlightNumber}");

            // Here we would normally check inventory first
            // Check for incomplete surface segment
            var lastSegment = pnr.Segments.LastOrDefault();

            if (lastSegment?.IsSurfaceSegment == true && string.IsNullOrEmpty(lastSegment.Destination))
            {
                lastSegment.Destination = sellSegmentRequest.From;

                // Calculate surface duration if needed
                var surfaceArrival = DateTimeHelper.CombineDateAndTime(
                    sellSegmentRequest.DepartureDate,
                    sellSegmentRequest.DepartureTime);

                lastSegment.ArrivalDate = surfaceArrival.ToString("ddMMM");
                lastSegment.ArrivalTime = surfaceArrival.ToString("HHmm");
            }

            var segment = new Segment
            {
                FlightNumber = sellSegmentRequest.FlightNumber.ToUpper(),
                Origin = sellSegmentRequest.From.ToUpper(),
                Destination = sellSegmentRequest.To.ToUpper(),
                DepartureDate = sellSegmentRequest.DepartureDate,
                ArrivalDate = sellSegmentRequest.ArrivalDate,
                DepartureTime = sellSegmentRequest.DepartureTime,
                ArrivalTime = sellSegmentRequest.ArrivalTime,
                BookingClass = sellSegmentRequest.BookingClass.ToUpper(),
                Quantity = sellSegmentRequest.Quantity,
                Status = SegmentStatus.Holding
            };

            pnr.Segments.Add(segment);

            return await Task.FromResult(pnr);
        }

        public async Task<Pnr> RemoveSegment(Pnr pnr, int segmentNumber)
        {
            if (segmentNumber <= 0 || segmentNumber > pnr.Segments.Count)
                throw new ArgumentException("Invalid segment number");

            var segment = pnr.Segments[segmentNumber - 1];

            // Return inventory
            if (!_inventoryService.IncrementInventory(segment.FlightNumber, segment.DepartureDate, segment.BookingClass, 1))
                throw new InvalidOperationException($"Failed to return inventory for {segment.FlightNumber}");

            // Remove the segment
            pnr.Segments.RemoveAt(segmentNumber - 1);

            return await Task.FromResult(pnr);
        }

        public async Task<Pnr> AddPhone(Pnr pnr, string phoneNumber, string type = "M")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number is required");

            pnr.Contact.PhoneNumber = phoneNumber;

            return await Task.FromResult(pnr);
        }

        public async Task<Pnr> AddEmail(Pnr pnr, string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                throw new ArgumentException("Email address is required");

            pnr.Contact.EmailAddress = emailAddress;

            return await Task.FromResult(pnr);
        }

        public async Task<Pnr> AddAgency(Pnr pnr, AgencyRequest request)
        {
            if (pnr == null) throw new ArgumentNullException(nameof(pnr));
            if (string.IsNullOrWhiteSpace(request.AgencyCode)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(request.AgencyCode));

            pnr.Agency = new AgencyInfo
            {
                AgencyCode = request.AgencyCode,
                IataNumber = request.IataNumber,
                AgentId = request.AgentId
            };

            return await Task.FromResult(pnr);
        }

        public async Task<Pnr> AddRemarks(Pnr pnr, string remarkText)
        {
            if (string.IsNullOrWhiteSpace(remarkText)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(remarkText));

            pnr.Remarks.Add(remarkText);

            return await Task.FromResult(pnr);
        }

        public async Task<Pnr> AddTicketArrangement(Pnr pnr, DateTime ticketTimeLimit, string validatingCarrier)
        {
            pnr.TicketingInfo = new TicketingInfo
            {
                TimeLimit = ticketTimeLimit,
                ValidatingCarrier = validatingCarrier
            };

            return await Task.FromResult(pnr);
        }

        public async Task<Pnr> CommitPnr(Pnr pnr)
        {
            // Validate PNR has minimum required elements
            if (!pnr.Passengers.Any())
                throw new InvalidOperationException("PNR must have at least one passenger");

            if (!pnr.Segments.Any())
                throw new InvalidOperationException("PNR must have at least one segment");

            if (!ValidatePnr(pnr))
                throw new Exception("PNR not valid");

            // Set all segments to Confirmed status
            foreach (var segment in pnr.Segments)
            {
                segment.Status = SegmentStatus.Confirmed;
            }

            if (string.IsNullOrEmpty(pnr.RecordLocator))
            {
                // This is a new PNR - generate record locator
                pnr.RecordLocator = await GenerateUniqueRecordLocator();

                // Update all OSIs with the record locator
                foreach (var osi in pnr.OtherServiceInformation)
                {
                    osi.PnrLocator = pnr.RecordLocator;
                }

                // Store in repository
                _pnrRepository.Pnrs.Add(pnr);
            }
            else
            {
                // This is an existing PNR - update it
                var existingPnrIndex = _pnrRepository.Pnrs.FindIndex(p => p.RecordLocator == pnr.RecordLocator);

                if (existingPnrIndex == -1)
                    throw new InvalidOperationException($"Cannot update PNR - record locator {pnr.RecordLocator} not found");

                // Update the existing PNR
                _pnrRepository.Pnrs[existingPnrIndex] = pnr;
            }

            return await Task.FromResult(pnr);
        }

        private bool ValidatePnr(Pnr pnr)
        {
            // 1. Basic null checks
            if (pnr == null)
                throw new ArgumentNullException(nameof(pnr));

            // 2. Validate Agency Info (new validation)
            if (pnr.Agency == null || string.IsNullOrEmpty(pnr.Agency.AgencyCode))
            {
                throw new InvalidOperationException("RECEIVED FROM INFORMATION MISSING - USE RF TO ADD");
            }

            // 3. Validate Passengers
            if (!pnr.Passengers.Any())
                throw new InvalidOperationException("PNR must have at least one passenger");

            foreach (var passenger in pnr.Passengers)
            {
                if (string.IsNullOrEmpty(passenger.LastName))
                    throw new InvalidOperationException("All passengers must have a last name");
                if (string.IsNullOrEmpty(passenger.FirstName))
                    throw new InvalidOperationException("All passengers must have a first name");
            }

            // 4. Validate Segments
            if (!pnr.Segments.Any())
                throw new InvalidOperationException("PNR must have at least one segment");

            foreach (var segment in pnr.Segments)
            {
                if (segment.IsSurfaceSegment)
                    continue; // Skip validation for ARNK segments

                if (string.IsNullOrEmpty(segment.FlightNumber))
                    throw new InvalidOperationException("All segments must have a flight number");
                if (string.IsNullOrEmpty(segment.Origin))
                    throw new InvalidOperationException("All segments must have an origin");
                if (string.IsNullOrEmpty(segment.Destination))
                    throw new InvalidOperationException("All segments must have a destination");
                if (segment.DepartureTime == default)
                    throw new InvalidOperationException("All segments must have a departure time");
                if (segment.ArrivalTime == default)
                    throw new InvalidOperationException("All segments must have an arrival time");
                if (string.IsNullOrEmpty(segment.BookingClass))
                    throw new InvalidOperationException("All segments must have a booking class");
            }

            // 5. Validate Contact Information
            if (pnr.Contact == null ||
                (string.IsNullOrEmpty(pnr.Contact.PhoneNumber) &&
                 string.IsNullOrEmpty(pnr.Contact.EmailAddress)))
            {
                throw new InvalidOperationException("PNR must have at least one contact method (phone or email)");
            }

            // 6. Validate Ticketing Information
            if (pnr.TicketingInfo == null || pnr.TicketingInfo.TimeLimit == default)
                throw new InvalidOperationException("PNR must have ticketing arrangements");

            // 7. Validate Logical Segment Order
            for (int i = 0; i < pnr.Segments.Count - 1; i++)
            {
                var currentSegment = pnr.Segments[i];
                var nextSegment = pnr.Segments[i + 1];

                // Skip connection validation if either segment is ARNK
                if (currentSegment.IsSurfaceSegment || nextSegment.IsSurfaceSegment)
                    continue;

                // Check connection points for air segments
                if (currentSegment.Destination != nextSegment.Origin)
                    throw new InvalidOperationException($"Invalid connection between segments {i + 1} and {i + 2}: {currentSegment.Destination} <> {nextSegment.Origin}");

                // Check minimum connection time (e.g., 60 minutes)
                var currentArrival = DateTimeHelper.CombineDateAndTime(currentSegment.ArrivalDate, currentSegment.ArrivalTime);
                var nextDeparture = DateTimeHelper.CombineDateAndTime(nextSegment.DepartureDate, nextSegment.DepartureTime);
                var connectionTime = nextDeparture - currentArrival;

                if (connectionTime <= TimeSpan.Zero)
                    throw new InvalidOperationException($"Invalid connection time between segments {i + 1} and {i + 2}");
            }

            // 8. Validate SSRs and OSIs if present
            foreach (var ssr in pnr.SpecialServiceRequests)
            {
                if (string.IsNullOrEmpty(ssr.Code))
                    throw new InvalidOperationException("All SSRs must have a service code");

                // Validate passenger reference if specified
                if (ssr.PassengerId > 0 && pnr.Passengers.All(p => Convert.ToInt32(p.PassengerId) != ssr.PassengerId))
                    throw new InvalidOperationException("SSR references non-existent passenger");
            }

            // All validations passed
            return true;
        }

        private async Task<string> GenerateUniqueRecordLocator()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int length = 6;
            var random = new Random();
            string recordLocator;

            do
            {
                recordLocator = new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_pnrRepository.Pnrs.Any(p => p.RecordLocator == recordLocator));

            return recordLocator;
        }

        public Task<Pnr?> RetrievePnr(string recordLocator)
        {
            return Task.FromResult(_pnrRepository.Pnrs.FirstOrDefault(item => item.RecordLocator == recordLocator));
        }

        public Task<List<Pnr>> RetrieveAllPnrs()
        {
            return Task.FromResult(_pnrRepository.Pnrs);
        }

        public async Task<List<Pnr>> RetrieveByName(string lastName, string firstName = null)
        {
            return await Task.FromResult(_pnrRepository.Pnrs
                .Where(pnr => pnr.Passengers.Any(p =>
                    p.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase) &&
                    (firstName == null || p.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase))))
                .ToList());
        }

        public async Task<List<Pnr>> RetrieveByFlight(string flightNumber, string date)
        {
            return await Task.FromResult(_pnrRepository.Pnrs
                .Where(pnr => pnr.Segments.Any(s =>
                    s.FlightNumber.Equals(flightNumber, StringComparison.OrdinalIgnoreCase) &&
                    s.DepartureDate.Equals(date, StringComparison.OrdinalIgnoreCase)))
                .ToList());
        }

        public async Task<List<Pnr>> RetrieveByPhone(string phoneNumber)
        {
            return await Task.FromResult(_pnrRepository.Pnrs
                .Where(pnr => pnr.Contact.PhoneNumber != null &&
                              pnr.Contact.PhoneNumber.Replace(" ", "").EndsWith(phoneNumber.Replace(" ", "")))
                .ToList());
        }

        public async Task<List<Pnr>> RetrieveByTicket(string ticketNumber)
        {
            return await Task.FromResult(_pnrRepository.Pnrs
                .Where(pnr => pnr.Tickets.Any(t =>
                    t.TicketNumber.Equals(ticketNumber, StringComparison.OrdinalIgnoreCase)))
                .ToList());
        }

        public async Task<List<Pnr>> RetrieveByFrequentFlyer(string ffNumber)
        {
            return await Task.FromResult(_pnrRepository.Pnrs
                .Where(pnr => pnr.Remarks.Any(r =>
                    r.Contains("FREQUENT FLYER") && r.Contains(ffNumber)))
                .ToList());
        }

        public async Task<(Segment segment, Pnr pnr)> SellSegment(Pnr pnr, FlightInventory flightToSell, string bookingClass, int quantity)
        {
            if (pnr == null)
            {
                pnr = await CreatePnrWorkspace(null); // Pass appropriate UserContext in real implementation
            }

            // Validate and decrement inventory
            if (!await ValidateAndDecrementInventory(flightToSell, bookingClass, quantity))
            {
                throw new InvalidOperationException($"Not enough {bookingClass} class seats available on {flightToSell.FlightNo}");
            }

            // Update any surface segments
            await UpdateSurfaceSegments(pnr, flightToSell);

            // Create and add the new segment
            var segment = new Segment
            {
                FlightNumber = flightToSell.FlightNo.ToUpper(),
                Origin = flightToSell.From.ToUpper(),
                Destination = flightToSell.To.ToUpper(),
                DepartureDate = flightToSell.DepartureDate,
                ArrivalDate = flightToSell.ArrivalDate,
                DepartureTime = flightToSell.DepartureTime,
                ArrivalTime = flightToSell.ArrivalTime,
                BookingClass = bookingClass.ToUpper(),
                Quantity = quantity,
                Status = SegmentStatus.Holding
            };

            pnr.Segments.Add(segment);

            return (segment, pnr);
        }

        private async Task<bool> ValidateAndDecrementInventory(FlightInventory flight, string bookingClass, int quantity)
        {
            // Check if enough seats are available
            if (!flight.Seats.ContainsKey(bookingClass) || flight.Seats[bookingClass] < quantity)
            {
                return false;
            }

            return _inventoryService.DecrementInventory(flight.FlightNo, flight.DepartureDate, bookingClass, quantity);
        }

        private async Task UpdateSurfaceSegments(Pnr pnr, FlightInventory newFlight)
        {
            // Check for incomplete surface segment
            var lastSegment = pnr.Segments.LastOrDefault();
            if (lastSegment?.IsSurfaceSegment == true && string.IsNullOrEmpty(lastSegment.Destination))
            {
                lastSegment.Destination = newFlight.From;

                // Calculate surface duration
                var surfaceArrival = DateTimeHelper.CombineDateAndTime(
                    newFlight.DepartureDate,
                    newFlight.DepartureTime);

                lastSegment.ArrivalDate = surfaceArrival.ToString("ddMMM");
                lastSegment.ArrivalTime = surfaceArrival.ToString("HHmm");
            }
        }

        public async Task<bool> DeletePnr(string recordLocator)
        {
            var pnr = _pnrRepository.Pnrs.FirstOrDefault(p => p.RecordLocator == recordLocator);
            if (pnr == null)
                throw new InvalidOperationException("PNR NOT FOUND");

            // Return inventory for each segment
            foreach (var segment in pnr.Segments.Where(s => !s.IsSurfaceSegment))
            {
                if (!_inventoryService.IncrementInventory(segment.FlightNumber, segment.DepartureDate, segment.BookingClass, segment.Quantity))
                {
                    throw new InvalidOperationException($"FAILED TO RETURN INVENTORY FOR {segment.FlightNumber}");
                }
            }

            // Release any assigned seats
            foreach (var seatAssignment in pnr.SeatAssignments)
            {
                var segment = pnr.Segments[int.Parse(seatAssignment.SegmentNumber) - 1];
                if (!await _inventoryService.ReleaseSeat(segment.FlightNumber, segment.DepartureDate, seatAssignment.SeatNumber))
                {
                    throw new InvalidOperationException($"FAILED TO RELEASE SEAT {seatAssignment.SeatNumber} ON {segment.FlightNumber}");
                }
            }

            // Delete the PNR
            if (!_pnrRepository.Pnrs.Remove(pnr))
            {
                throw new InvalidOperationException("FAILED TO DELETE PNR");
            }

            return true;
        }



        //private async Task<SellSegmentRequest> ParseLongSellFormat(string command)
        //{
        //    try
        //    {
        //        // Parse long sell format: SS VS001Y24NOVLHRJFK1
        //        string flightNumber = command.Substring(3, 5).Trim(); // VS001
        //        string bookingClass = command.Substring(8, 1); // Y
        //        string date = command.Substring(9, 5); // 24NOV
        //        string from = command.Substring(14, 3); // LHR
        //        string to = command.Substring(17, 3); // JFK
        //        int quantity = int.Parse(command.Substring(20, 1)); // 1

        //        // Get flight details from inventory
        //        var flight = (await _inventoryService.SearchAvailability(new AvailabilityRequest
        //        {
        //            DepartureDate = date,
        //            Origin = from,
        //            Destination = to
        //        })).FirstOrDefault(f => f.FlightNo == flightNumber);

        //        if (flight == null)
        //            throw new ArgumentException($"Flight {flightNumber} not found");

        //        return new SellSegmentRequest
        //        {
        //            FlightNumber = flight.FlightNo,
        //            BookingClass = bookingClass,
        //            From = flight.From,
        //            To = flight.To,
        //            DepartureDate = flight.DepartureDate,
        //            ArrivalDate = flight.ArrivalDate,
        //            DepartureTime = flight.DepartureTime,
        //            ArrivalTime = flight.ArrivalTime,
        //            Quantity = quantity
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new ArgumentException("Invalid long sell format - Use SS VS001Y24NOVLHRJFK1", ex);
        //    }
        //}

        //private async Task<SellSegmentRequest> ParseShortSellFormat(string command, List<FlightInventory> searchedFlights)
        //{
        //    try
        //    {
        //        int quantity = int.Parse(command.Substring(2, 1));
        //        string bookingClass = command.Substring(3, 1);
        //        int lineNumber = int.Parse(command.Substring(4));

        //        if (searchedFlights == null || !searchedFlights.Any())
        //            throw new InvalidOperationException("No flights available from previous search");

        //        var availableFlight = searchedFlights.ElementAtOrDefault(lineNumber - 1);
        //        if (availableFlight == null)
        //            throw new ArgumentException("Invalid flight selection");

        //        return new SellSegmentRequest
        //        {
        //            FlightNumber = availableFlight.FlightNo,
        //            BookingClass = bookingClass,
        //            From = availableFlight.From,
        //            To = availableFlight.To,
        //            DepartureDate = availableFlight.DepartureDate,
        //            ArrivalDate = availableFlight.ArrivalDate,
        //            DepartureTime = availableFlight.DepartureTime,
        //            ArrivalTime = availableFlight.ArrivalTime,
        //            Quantity = quantity
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new ArgumentException("Invalid short sell format - Use SS1Y2", ex);
        //    }
        //}

        //private async Task<bool> ValidateInventory(SellSegmentRequest request)
        //{
        //    var flight = await _inventoryService.FindFlight(request.FlightNumber, request.DepartureDate);
        //    if (flight == null)
        //        return false;

        //    return _inventoryService.DecrementInventory(
        //        request.FlightNumber,
        //        request.DepartureDate,
        //        request.BookingClass,
        //        request.Quantity);
        //}

    }
}