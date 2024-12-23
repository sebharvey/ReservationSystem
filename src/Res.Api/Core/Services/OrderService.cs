using Res.Core.Interfaces;
using Res.Api.Models.FlightBooking.Models;
using Res.Api.Common.Helpers;
using Res.Api.Models;
using Res.Application.Interfaces;
using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;
using Microsoft.Extensions.Logging;
using Res.Core.Common.Helpers;
using Res.Domain.Entities.CheckIn;
using Res.Domain.Requests;

namespace Res.Api.Core.Services
{
    public class OrderService : IOrderService
    {
        private readonly ILogger<OrderService> _logger;

        private readonly IReservationService _reservationService;
        private readonly IReservationCommands _reservationCommands;
        private readonly IUserService _userService;
        private readonly IInventoryService _inventoryService;
        private readonly IBoardingPassService _boardingPassService;

        public OrderService(
            IReservationService reservationService,
            IReservationCommands reservationCommands,
            IUserService userService,
            IInventoryService inventoryService, ILogger<OrderService> logger, IBoardingPassService boardingPassService)
        {
            _reservationService = reservationService;
            _reservationCommands = reservationCommands;
            _userService = userService;
            _inventoryService = inventoryService;
            _logger = logger;
            _boardingPassService = boardingPassService;
        }

        public async Task<OrderCreateResponse> Create(OrderCreateRequest orderCreateRequest)
        {
            string recordLocator = null;

            try
            {
                // Create dummy user context for system actions
                _reservationCommands.User = new Domain.Responses.UserContext
                {
                    UserId = "SYSTEM",
                    Role = "System",
                    AgentId = "SYS001",
                    SessionId = Guid.NewGuid() // Create a new session for every web request
                };

                // 1. Create new PNR workspace
                await _reservationService.CreatePnrWorkspace();

                // 2. Add passengers
                foreach (var passenger in orderCreateRequest.Passengers)
                {
                    await _reservationCommands.ProcessAddName($"NM1{passenger.LastName}/{passenger.FirstName} {passenger.Title}");
                }

                // 3. Add outbound flight segment using long sell format
                var outboundDetails = OfferIdHelper.Decode(orderCreateRequest.OutboundOfferId).Split('-');
                var flightNumber = outboundDetails[0];
                var departureDate = outboundDetails[1];
                var bookingClass = outboundDetails[2];

                // Look up flight details
                var outboundFlight = await _inventoryService.FindFlight(flightNumber, departureDate);
                if (outboundFlight == null)
                {
                    throw new Exception($"Flight {flightNumber} on {departureDate} not found");
                }

                var outboundCommand = $"SS {flightNumber}{bookingClass}{departureDate}{outboundFlight.From}{outboundFlight.To}{orderCreateRequest.PassengerCount}";
                await _reservationCommands.ProcessSellSegment(outboundCommand);

                // 4. Add inbound flight segment if exists
                if (!string.IsNullOrEmpty(orderCreateRequest.InboundOfferId))
                {
                    var inboundDetails = OfferIdHelper.Decode(orderCreateRequest.InboundOfferId).Split('-');
                    var returnFlightNumber = inboundDetails[0];
                    var returnDepartureDate = inboundDetails[1];
                    var returnBookingClass = inboundDetails[2];

                    var inboundFlight = await _inventoryService.FindFlight(returnFlightNumber, returnDepartureDate);
                    if (inboundFlight == null)
                    {
                        throw new Exception($"Flight {returnFlightNumber} on {returnDepartureDate} not found");
                    }

                    var inboundCommand = $"SS {returnFlightNumber}{returnBookingClass}{returnDepartureDate}{inboundFlight.From}{inboundFlight.To}{orderCreateRequest.PassengerCount}";
                    await _reservationCommands.ProcessSellSegment(inboundCommand);
                }

                // 5. Add contact details
                await _reservationCommands.ProcessContact($"CTCP {orderCreateRequest.ContactDetails.Phone}");
                await _reservationCommands.ProcessContact($"CTCE {orderCreateRequest.ContactDetails.Email}");

                // 6. Add ticketing time limit (24 hours from now)
                var ticketLimit = DateTime.Now.AddHours(24);
                await _reservationCommands.AddTicketingArrangement($"TLTL{ticketLimit:ddMMM}/VS");

                // 7. Add loyalty number if provided
                if (!string.IsNullOrEmpty(orderCreateRequest.ContactDetails.LoyaltyNumber))
                {
                    await _reservationCommands.AddRemark($"RM FREQUENT FLYER {orderCreateRequest.ContactDetails.LoyaltyNumber}");
                }

                // 8. Price the PNR and store fares
                await _reservationCommands.ProcessPricePnr("FXP");
                await _reservationCommands.ProcessStoreFare("FS"); // Store the lowest fare option

                // 9. Add form of payment
                var fop = $"FP*CC/{orderCreateRequest.PaymentDetails.CardType.ToUpper()}/" +
                          $"{orderCreateRequest.PaymentDetails.CardNumber}/" +
                          $"{orderCreateRequest.PaymentDetails.Expiry.Replace("/", string.Empty)}/GBP100.00"; // Amount should come from pricing
                await _reservationCommands.ProcessFormOfPayment(fop);

                // 10. End transaction to get PNR record locator
                var result = await _reservationCommands.ProcessEndTransactionAndRecall();

                // Parse record locator from response
                var pnrResponse = (Pnr)result.Response;
                recordLocator = pnrResponse.RecordLocator;

                // 11. Issue tickets
                try
                {
                    var ticketingResponse = await _reservationCommands.ProcessTicketing();

                    if (!ticketingResponse.Success)
                        throw new Exception(ticketingResponse.Message);
                }
                catch (Exception ex)
                {
                    // If ticketing fails, delete the PNR and clean up
                    if (recordLocator != null)
                    {
                        await _reservationService.DeletePnr(recordLocator);
                    }

                    throw new Exception($"Ticketing failed: {ex.Message}. Booking has been cancelled.");
                }

                // 12 Add seat selections
                if (orderCreateRequest.Seats?.Any() == true)
                {
                    // Group seat selections by OfferId (segment)
                    var seatsBySegment = orderCreateRequest.Seats
                        .GroupBy(s => s.OfferId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var segmentSeats in seatsBySegment)
                    {
                        try
                        {
                            // Decode the offer ID to get flight details
                            var segmentDetails = OfferIdHelper.Decode(segmentSeats.Key).Split('-');
                            var segmentFlightNumber = segmentDetails[0];
                            var segmentDepartureDate = segmentDetails[1];

                            // Find matching segment number in PNR
                            var segmentNumber = pnrResponse.Data.Segments
                                .Select((segment, index) => new { segment, index = index + 1 })
                                .FirstOrDefault(s =>
                                    s.segment.FlightNumber == segmentFlightNumber &&
                                    s.segment.DepartureDate == segmentDepartureDate)
                                ?.index;

                            if (!segmentNumber.HasValue)
                            {
                                throw new Exception($"Cannot find matching segment for flight {segmentFlightNumber} on {segmentDepartureDate}");
                            }

                            // Process each seat selection for this segment
                            foreach (var seatSelection in segmentSeats.Value)
                            {
                                // Validate passenger number is within range
                                if (seatSelection.Passenger < 1 || seatSelection.Passenger > pnrResponse.Data.Passengers.Count)
                                {
                                    throw new Exception($"Invalid passenger number {seatSelection.Passenger}. Must be between 1 and {pnrResponse.Data.Passengers.Count}");
                                }

                                // Get the passenger (adjusting for 1-based indexing)
                                var passenger = pnrResponse.Data.Passengers[seatSelection.Passenger - 1];

                                // Format and process the seat assignment command
                                var seatCommand = $"ST/{seatSelection.Seat}/P{passenger.PassengerId}/S{segmentNumber}";
                                var seatResponse = await _reservationCommands.ProcessAssignSeat(seatCommand);

                                if (!seatResponse.Success)
                                {
                                    throw new Exception($"Seat assignment failed for passenger {seatSelection.Passenger}: {seatResponse.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to assign seats for segment {SegmentKey}", segmentSeats.Key);
                            // Continue with other segments even if one fails
                            continue;
                        }
                    }
                }

                return new OrderCreateResponse
                {
                    Success = true,
                    BookingConfirmation = recordLocator,
                    Message = "Booking created successfully"
                };
            }
            catch (Exception ex)
            {
                // If we have a record locator but something failed after PNR creation
                if (recordLocator != null)
                {
                    try
                    {
                        await _reservationService.DeletePnr(recordLocator);
                    }
                    catch (Exception cleanupEx)
                    {
                        // Log the cleanup failure but return the original error to the user
                        // In production, you'd want to log this properly
                        Console.WriteLine($"Failed to cleanup PNR {recordLocator}: {cleanupEx.Message}");
                    }
                }

                return new OrderCreateResponse
                {
                    Success = false,
                    Message = $"Failed to create booking: {ex.Message}"
                };
            }
        }

        public async Task<OrderRetrieveResponse> Retrieve(OrderRetrieveRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.RecordLocator) || string.IsNullOrEmpty(request.LastName))
                {
                    return new OrderRetrieveResponse
                    {
                        Success = false,
                        Message = "Booking reference and last name are required"
                    };
                }

                // Retrieve PNR
                await _reservationService.RetrievePnr(request.RecordLocator.ToUpper());

                // Validate PNR exists and is ticketed
                if (_reservationService.Pnr == null)
                {
                    return new OrderRetrieveResponse
                    {
                        Success = false,
                        Message = "Booking not found"
                    };
                }

                if (_reservationService.Pnr.Data.Status != PnrStatus.Ticketed)
                {
                    return new OrderRetrieveResponse
                    {
                        Success = false,
                        Message = "Booking not ticketed"
                    };
                }

                // Verify last name matches a passenger
                bool lastNameMatches = _reservationService.Pnr.Data.Passengers.Any(p => p.LastName.Equals(request.LastName, StringComparison.OrdinalIgnoreCase));

                if (!lastNameMatches)
                {
                    return new OrderRetrieveResponse
                    {
                        Success = false,
                        Message = "Last name does not match booking"
                    };
                }

                // Map PNR to response
                return new OrderRetrieveResponse
                {
                    Success = true,
                    RecordLocator = _reservationService.Pnr.RecordLocator,
                    Status = _reservationService.Pnr.Data.Status.ToString(),
                    Tickets = _reservationService.Pnr.Data.Tickets,
                    BoardingPasses = GetValidBoardingPasses(_reservationService.Pnr), 
                    Passengers = _reservationService.Pnr.Data.Passengers.Select(p => new OrderRetrieveResponse.PassengerInfo
                    {
                        Title = p.Title,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        TicketNumber = _reservationService.Pnr.Data.Tickets.FirstOrDefault(t => t.PassengerId == p.PassengerId)?.TicketNumber
                    }).ToList(),
                    Segments = _reservationService.Pnr.Data.Segments.Select(s => new OrderRetrieveResponse.SegmentInfo
                    {
                        FlightNumber = s.FlightNumber,
                        Origin = s.Origin,
                        Destination = s.Destination,
                        DepartureDate = s.DepartureDate,
                        DepartureTime = s.DepartureTime,
                        ArrivalDate = s.ArrivalDate,
                        ArrivalTime = s.ArrivalTime,
                        BookingClass = s.BookingClass,
                        Status = s.Status.ToString()
                    }).ToList(),
                    Contact = new OrderRetrieveResponse.ContactInfo
                    {
                        PhoneNumber = _reservationService.Pnr.Data.Contact.PhoneNumber,
                        EmailAddress = _reservationService.Pnr.Data.Contact.EmailAddress
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booking {RecordLocator}", request.RecordLocator);

                return new OrderRetrieveResponse
                {
                    Success = false,
                    Message = "An error occurred retrieving the booking"
                };
            }
        }

        private List<BoardingPass> GetValidBoardingPasses(Pnr pnr)
        {
            var boardingPasses = new List<BoardingPass>();

            // Only proceed if PNR has tickets
            if (!pnr.Data.Tickets.Any())
                return boardingPasses;

            // Process each segment
            foreach (var segment in pnr.Data.Segments.Where(s => !s.IsSurfaceSegment))
            {
                // Get passengers with valid tickets and checked-in coupons for this segment
                var validPassengers = pnr.Data.Tickets
                    .Where(ticket => ticket.Coupons.Any(coupon =>
                        coupon.FlightNumber == segment.FlightNumber &&
                        coupon.DepartureDate == segment.DepartureDate &&
                        coupon.Status == CouponStatus.CheckedIn))
                    .Select(ticket => new
                    {
                        Ticket = ticket,
                        Passenger = pnr.Data.Passengers.First(p => p.PassengerId == ticket.PassengerId),
                        SeatAssignment = pnr.Data.SeatAssignments.FirstOrDefault(sa =>
                            sa.PassengerId == ticket.PassengerId &&
                            sa.SegmentNumber == (pnr.Data.Segments.IndexOf(segment) + 1).ToString())
                    });

                // Generate boarding pass for each valid passenger
                foreach (var passengerInfo in validPassengers)
                {
                    // Find any SSRs for this passenger
                    var ssrCodes = pnr.Data.SpecialServiceRequests
                        .Where(ssr => ssr.PassengerId == passengerInfo.Passenger.PassengerId)
                        .Select(ssr => ssr.Code)
                        .ToList();

                    // Create boarding pass request
                    var request = new BoardingPassRequest
                    {
                        RecordLocator = pnr.RecordLocator,
                        PassengerId = passengerInfo.Passenger.PassengerId,
                        TicketNumber = passengerInfo.Ticket.TicketNumber,
                        FlightNumber = segment.FlightNumber,
                        Origin = segment.Origin,
                        Destination = segment.Destination,
                        DepartureDate = segment.DepartureDate,
                        DepartureTime = segment.DepartureTime,
                        SeatNumber = passengerInfo.SeatAssignment?.SeatNumber,
                        SsrCodes = ssrCodes,

                        // Can add additional fields if needed:
                        // DepartureGate = ...,
                        // HasCheckedBags = ...,
                        // BaggageCount = ...,
                        // BaggageWeight = ...
                    };

                    try
                    {
                        var boardingPass = _boardingPassService.GenerateBoardingPass(request);
                        boardingPasses.Add(boardingPass);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate boarding pass for passenger {PassengerId} on {FlightNumber}",
                            passengerInfo.Passenger.PassengerId, segment.FlightNumber);
                        // Continue with next passenger even if one fails
                        continue;
                    }
                }
            }

            return boardingPasses;
        }
    }
}