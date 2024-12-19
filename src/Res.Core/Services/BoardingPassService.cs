using System.Globalization;
using Res.Core.Interfaces;
using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Pnr;
using Res.Domain.Entities.Ticket;
using Res.Domain.Enums;
using Res.Domain.Requests;

namespace Res.Core.Services
{
    public class BoardingPassService : IBoardingPassService
    {
        // Constants for barcode generation
        private const string FrequentFlyerTier = "LEE"; // Loyalty program tier
        private const string SecurityData = "O";  // International security status
        private const string CheckinSource = "1OS"; // Check-in source identifier
        private const string PnrConditionCode = "02"; // PNR conditional status
        private const string PassengerStatus = "0"; // Passenger status code
        private const string SourceOfBoardingPass = "2"; // Source of boarding pass issuance
        private const string BoardingPassIssueYear = "3"; // Year of issue
        private const string VersionNumber = "G"; // Format version number
        private const string FieldSize = "091"; // Fixed size of the document

        private readonly IReservationService _reservationService;

        public BoardingPassService(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        public BoardingPass GenerateBoardingPass(BoardingPassRequest request)
        {
            // Get PNR to access passenger details needed for boarding pass
            var pnr = _reservationService.RetrievePnr(request.RecordLocator).Result;
            if (pnr == null)
                throw new Exception("PNR NOT FOUND");

            var passenger = pnr.Passengers.FirstOrDefault(p => Convert.ToInt32(p.PassengerId) == request.PassengerId);
            if (passenger == null)
                throw new Exception("PASSENGER NOT FOUND");

            var segment = pnr.Segments.FirstOrDefault(s => s.FlightNumber == request.FlightNumber);
            if (segment == null)
                throw new Exception("SEGMENT NOT FOUND");

            var ticket = pnr.Tickets.FirstOrDefault(t => t.TicketNumber == request.TicketNumber);
            if (ticket == null)
                throw new Exception("TICKET NOT FOUND");

            var boardingPass = new BoardingPass
            {
                PassengerId = request.PassengerId,
                TicketNumber = request.TicketNumber,

                FlightNumber = segment.FlightNumber,
                Origin = segment.Origin,
                Destination = segment.Destination,
                DepartureDate = segment.DepartureDate,
                DepartureTime = segment.DepartureTime,
                DepartureGate = request.DepartureGate,
                BookingClass = segment.BookingClass,

                SeatNumber = request.SeatNumber,
                HasCheckedBags = request.HasCheckedBags,
                BaggageWeight = request.BaggageWeight,
                BaggageCount = request.BaggageCount,

                BoardingGroup = DetermineBoardingGroup(ticket, passenger),
                Sequence = GenerateSequenceNumber(),
                CheckInTime = DateTime.UtcNow,
                Status = CheckInStatus.CheckedIn
            };

            // Generate barcode data
            boardingPass.BarcodeData = GenerateBarcodeData(passenger, ticket, segment, request.SeatNumber);

            // Add special services if provided
            if (request.SsrCodes?.Any() == true)
            {
                AddSpecialServices(boardingPass, pnr.SpecialServiceRequests, request.SsrCodes);
            }

            // Add fast track/lounge access based on ticket class
            AddPremiumServices(boardingPass, segment.BookingClass);

            return boardingPass;
        }

        private string GenerateBarcodeData(Passenger passenger, Ticket ticket, Segment segment, string seatNumber)
        {
            // Format passenger name (up to 20 chars)
            var nameParts = $"{passenger.LastName}/{passenger.FirstName}".PadRight(20)[..20];

            // Format electronic ticket number (last 7 chars)
            var eTicket = ticket.TicketNumber.Substring(Math.Max(0, ticket.TicketNumber.Length - 7));

            // Calculate Julian date
            var julianDate = DateTime.ParseExact(segment.DepartureDate, "ddMMM", CultureInfo.InvariantCulture)
                .DayOfYear.ToString("D3");

            // Get airline codes
            var airlineDesignator = segment.FlightNumber.Substring(0, 2);
            var operatingCarrier = ticket.ValidatingCarrier;

            // Format class and seat
            var bookingClass = segment.BookingClass;
            var seatNum = seatNumber.PadRight(4)[..4];

            // Use bag tag number if bags checked
            var bagTagNumber = ticket.TicketNumber.Substring(Math.Max(0, ticket.TicketNumber.Length - 10));

            // Generate sequence number
            var sequenceNumber = new Random().Next(1000, 9999).ToString();

            return $"M1{nameParts} {eTicket} {segment.Origin}{segment.Destination}{airlineDesignator} " +
                   $"{julianDate} {bookingClass}{seatNum}{PassengerStatus}{SourceOfBoardingPass}" +
                   $"{FieldSize} {sequenceNumber}>{BoardingPassIssueYear}{SecurityData}{FieldSize} " +
                   $"{operatingCarrier} {bagTagNumber}{CheckinSource}{PnrConditionCode}{VersionNumber}";
        }

        private string DetermineBoardingGroup(Ticket ticket, Passenger passenger)
        {
            return ticket.Coupons.First().BookingClass switch
            {
                "J" => "1", // Business Class
                "W" => "2", // Premium Economy
                "Y" => "3", // Full Fare Economy
                _ => "4"    // Other Economy
            };
        }

        private string GenerateSequenceNumber()
        {
            return new Random().Next(1000, 9999).ToString();
        }

        private void AddSpecialServices(BoardingPass boardingPass, List<Ssr> ssrs, List<string> ssrCodes)
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
                }
            }

            foreach (var ssrCode in ssrCodes)
            {
                boardingPass.SecurityMessages.Add($"CHECK-IN SSR: {ssrCode}");
            }
        }

        private void AddPremiumServices(BoardingPass boardingPass, string bookingClass)
        {
            switch (bookingClass)
            {
                case "J": // Business Class
                    boardingPass.FastTrack = "ELIGIBLE";
                    boardingPass.LoungeAccess = "CLUBHOUSE";
                    break;

                case "W": // Premium Economy
                    boardingPass.FastTrack = "ELIGIBLE";
                    boardingPass.LoungeAccess = "NO ACCESS";
                    break;

                default:
                    boardingPass.FastTrack = "NOT ELIGIBLE";
                    boardingPass.LoungeAccess = "NO ACCESS";
                    break;
            }
        }
    }
}