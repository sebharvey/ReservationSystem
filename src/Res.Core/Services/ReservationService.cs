using Res.Core.Common.Helpers;
using Res.Core.Interfaces;
using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;
using Res.Domain.Requests;
using Res.Domain.Responses;
using Res.Infrastructure.Repositories;

namespace Res.Core.Services
{
    public class ReservationService : IReservationService
    {
        public UserContext UserContext { get; set; }
        public Pnr? Pnr { get; set; }

        private readonly IPnrRepository _pnrRepository;
        private readonly IInventoryService _inventoryService;

        public ReservationService(IPnrRepository pnrRepository, IInventoryService inventoryService)
        {
            _pnrRepository = pnrRepository;
            _inventoryService = inventoryService;
        }

        public async Task<bool> CreatePnrWorkspace()
        {
            if (UserContext == null)
                throw new InvalidOperationException("No user context specified");

            Pnr = new Pnr
            {
                SessionId = UserContext.SessionId,
                SessionTimestamp = DateTime.Now,

                Data = new Pnr.PnrData
                {
                    Status = PnrStatus.Pending,
                    Passengers = new List<Passenger>(),
                    Segments = new List<Segment>(),
                    Agency = new AgencyInfo { AgencyCode = "VS", AgentId = UserContext.UserId, OfficeId = "LON1A2BC3" }, // TODO this needs to be set from the logged in user account
                    Contact = new ContactInfo(),
                    Remarks = new List<string>(),
                    TicketingInfo = new TicketingInfo(),
                    SpecialServiceRequests = new List<Ssr>(),
                    OtherServiceInformation = new List<Osi>()
                }
            };

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<bool> AddName(string lastName, string firstName, string title, PassengerType type)
        {
            if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("Last name and first name are required");

            var passenger = new Passenger
            {
                PassengerId = Pnr.Data.Passengers.Count + 1,
                LastName = lastName.ToUpper(),
                FirstName = firstName.ToUpper(),
                Title = title?.ToUpper() ?? string.Empty,
                Type = type,
                Documents = new List<Document>()
            };

            Pnr.Data.Passengers.Add(passenger);

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<bool> AddSegment(SellSegmentRequest sellSegmentRequest)
        {
            // Check and decrement inventory before adding segment
            if (!_inventoryService.DecrementInventory(sellSegmentRequest.FlightNumber, sellSegmentRequest.DepartureDate, sellSegmentRequest.BookingClass, sellSegmentRequest.Quantity))
                throw new InvalidOperationException($"Not enough {sellSegmentRequest.BookingClass} class seats available on {sellSegmentRequest.FlightNumber}");

            // Here we would normally check inventory first
            // Check for incomplete surface segment
            var lastSegment = Pnr.Data.Segments.LastOrDefault();

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

            Pnr.Data.Segments.Add(segment);

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<bool> RemoveSegment(int segmentNumber)
        {
            if (segmentNumber <= 0 || segmentNumber > Pnr.Data.Segments.Count)
                throw new ArgumentException("Invalid segment number");

            var segment = Pnr.Data.Segments[segmentNumber - 1];

            // Return inventory
            if (!_inventoryService.IncrementInventory(segment.FlightNumber, segment.DepartureDate, segment.BookingClass, 1))
                throw new InvalidOperationException($"Failed to return inventory for {segment.FlightNumber}");

            // Remove the segment
            Pnr.Data.Segments.RemoveAt(segmentNumber - 1);

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<bool> AddPhone(string phoneNumber, string type = "M")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number is required");

            Pnr.Data.Contact.PhoneNumber = phoneNumber;

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<bool> AddEmail(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                throw new ArgumentException("Email address is required");

            Pnr.Data.Contact.EmailAddress = emailAddress;

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<bool> AddAgency(AgencyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AgencyCode)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(request.AgencyCode));

            Pnr.Data.Agency = new AgencyInfo
            {
                AgencyCode = request.AgencyCode,
                IataNumber = request.IataNumber,
                AgentId = request.AgentId
            };

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<bool> AddRemarks(string remarkText)
        {
            if (string.IsNullOrWhiteSpace(remarkText)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(remarkText));

            Pnr.Data.Remarks.Add(remarkText);

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<bool> AddTicketArrangement(DateTime ticketTimeLimit, string validatingCarrier)
        {
            Pnr.Data.TicketingInfo = new TicketingInfo
            {
                TimeLimit = ticketTimeLimit,
                ValidatingCarrier = validatingCarrier
            };

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<bool> CommitPnr()
        {
            // Validate PNR has minimum required elements
            if (Pnr == null)
                throw new ArgumentNullException(nameof(Pnr));
            
            if (!Pnr.Data.Passengers.Any())
                throw new InvalidOperationException("PNR must have at least one passenger");

            if (!Pnr.Data.Segments.Any())
                throw new InvalidOperationException("PNR must have at least one segment");

            if (!ValidatePnr())
                throw new Exception("PNR not valid");

            // Set all segments to Confirmed status
            foreach (var segment in Pnr.Data.Segments)
            {
                segment.Status = SegmentStatus.Confirmed;
            }

            await _pnrRepository.Save(Pnr, true);

            // Update all OSIs with the record locator
            foreach (var osi in Pnr.Data.OtherServiceInformation)
            {
                osi.PnrLocator = Pnr.RecordLocator;
            }

            Pnr.SessionId = null;
            Pnr.SessionTimestamp = null;

            await _pnrRepository.Save(Pnr);

            return true;
        }

        private bool ValidatePnr()
        {
            // 1. Basic null checks
            if (Pnr == null)
                throw new ArgumentNullException(nameof(Pnr));

            // 2. Validate Agency Info (new validation)
            if (Pnr.Data.Agency == null || string.IsNullOrEmpty(Pnr.Data.Agency.AgencyCode))
            {
                throw new InvalidOperationException("RECEIVED FROM INFORMATION MISSING - USE RF TO ADD");
            }

            // 3. Validate Passengers
            if (!Pnr.Data.Passengers.Any())
                throw new InvalidOperationException("PNR must have at least one passenger");

            foreach (var passenger in Pnr.Data.Passengers)
            {
                if (string.IsNullOrEmpty(passenger.LastName))
                    throw new InvalidOperationException("All passengers must have a last name");
                if (string.IsNullOrEmpty(passenger.FirstName))
                    throw new InvalidOperationException("All passengers must have a first name");
            }

            // 4. Validate Segments
            if (!Pnr.Data.Segments.Any())
                throw new InvalidOperationException("PNR must have at least one segment");

            foreach (var segment in Pnr.Data.Segments)
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
            if (Pnr.Data.Contact == null ||
                (string.IsNullOrEmpty(Pnr.Data.Contact.PhoneNumber) &&
                 string.IsNullOrEmpty(Pnr.Data.Contact.EmailAddress)))
            {
                throw new InvalidOperationException("PNR must have at least one contact method (phone or email)");
            }

            // 6. Validate Ticketing Information
            if (Pnr.Data.TicketingInfo == null || Pnr.Data.TicketingInfo.TimeLimit == default)
                throw new InvalidOperationException("PNR must have ticketing arrangements");

            // 7. Validate Logical Segment Order
            for (int i = 0; i < Pnr.Data.Segments.Count - 1; i++)
            {
                var currentSegment = Pnr.Data.Segments[i];
                var nextSegment = Pnr.Data.Segments[i + 1];

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
            foreach (var ssr in Pnr.Data.SpecialServiceRequests)
            {
                if (string.IsNullOrEmpty(ssr.Code))
                    throw new InvalidOperationException("All SSRs must have a service code");

                // Validate passenger reference if specified
                if (ssr.PassengerId > 0 && Pnr.Data.Passengers.All(p => Convert.ToInt32(p.PassengerId) != ssr.PassengerId))
                    throw new InvalidOperationException("SSR references non-existent passenger");
            }

            // All validations passed
            return true;
        }

        public async Task<bool> RetrievePnr(string recordLocator)
        {
            await _pnrRepository.GetByRecordLocator(recordLocator.ToUpper());

            if (Pnr == null)
                throw new InvalidOperationException($"No PNR loaded - {recordLocator}");

            Pnr.SessionId = UserContext.SessionId;
            Pnr.SessionTimestamp = DateTime.Now;

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task<List<Pnr>> RetrieveAllPnrs()
        {
            return await _pnrRepository.GetAll();
        }

        public async Task<List<Pnr>> RetrieveByName(string lastName, string firstName = null)
        {
            return await _pnrRepository.GetAll(); // TODO need to implement proper logic that filters out based on params
        }

        public async Task<List<Pnr>> RetrieveByFlight(string flightNumber, string date)
        {
            return await _pnrRepository.GetAll(); // TODO need to implement proper logic that filters out based on params
        }

        public async Task<List<Pnr>> RetrieveByPhone(string phoneNumber)
        {
            return await _pnrRepository.GetAll(); // TODO need to implement proper logic that filters out based on params
        }

        public async Task<List<Pnr>> RetrieveByTicket(string ticketNumber)
        {
            return await _pnrRepository.GetAll(); // TODO need to implement proper logic that filters out based on params
        }

        public async Task<List<Pnr>> RetrieveByFrequentFlyer(string ffNumber)
        {
            return await _pnrRepository.GetAll(); // TODO need to implement proper logic that filters out based on params
        }

        public async Task<Segment> SellSegment(FlightInventory flightToSell, string bookingClass, int quantity)
        {
            if (Pnr == null)
            {
                await CreatePnrWorkspace();
            }

            // Validate and decrement inventory
            if (!await ValidateAndDecrementInventory(flightToSell, bookingClass, quantity))
            {
                throw new InvalidOperationException($"Not enough {bookingClass} class seats available on {flightToSell.FlightNo}");
            }

            // Update any surface segments
            await UpdateSurfaceSegments(flightToSell);

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

            Pnr.Data.Segments.Add(segment);

            await _pnrRepository.Save(Pnr);

            return segment;
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

        private async Task UpdateSurfaceSegments(FlightInventory newFlight)
        {
            // Check for incomplete surface segment
            var lastSegment = Pnr.Data.Segments.LastOrDefault();
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
            throw new NotImplementedException();
        }

        public async Task<Pnr?> LoadCurrentPnr()
        {
            if (UserContext == null)
                return null;

            return await _pnrRepository.GetBySessionId(UserContext.SessionId);
        }

        public async Task<bool> AddArnkSegment(int? position)
        {
            // Create a surface segment
            var surfaceSegment = new Segment
            {
                FlightNumber = "ARNK",
                Status = SegmentStatus.Confirmed,
                IsSurfaceSegment = true,
                Quantity = Pnr.Data.Passengers.Count
            };

            // If there are existing segments, connect this surface segment between them
            if (Pnr.Data.Segments.Count > 0)
            {
                var lastSegment = Pnr.Data.Segments.Last();
                surfaceSegment.Origin = lastSegment.Destination;

                // The destination will be populated when the next flight segment is added
                surfaceSegment.DepartureDate = lastSegment.ArrivalDate;
                surfaceSegment.DepartureTime = lastSegment.ArrivalTime;
            }

            Pnr.Data.Segments.Add(surfaceSegment);

            await _pnrRepository.Save(Pnr);

            return true;
        }

        public async Task IgnoreSession()
        {
            // Clear the current workspace
            Pnr = null;

            //Load the one with the current session ID and remove the session data
            var pnr = await _pnrRepository.GetBySessionId(UserContext.SessionId);

            if (pnr != null)
            {
                pnr.SessionId = null;
                pnr.SessionTimestamp = null;

                await _pnrRepository.Save(pnr);
            }
        }
    }
}