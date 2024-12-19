using Microsoft.Extensions.Logging;
using Res.Core.Interfaces;
using Res.Domain.Dto;
using Res.Domain.Entities.Pnr;
using Res.Domain.Entities.Ticket;
using Res.Domain.Enums;
using Res.Domain.Exceptions;

namespace Res.Core.Services
{
    public class TicketingService : ITicketingService
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<TicketingService> _logger;
        private static int _ticketCounter = 1000000;
        private readonly object _lockObj = new();

        private readonly Dictionary<string, string> _airlineNumericCodes = new()
        {
            { "VS", "932" }, // Virgin Atlantic
            { "BA", "125" }, // British Airways
            { "AA", "001" }, // American Airlines
            { "UA", "016" }, // United Airlines
            { "DL", "006" }, // Delta Airlines
            { "LH", "220" }, // Lufthansa
            { "AF", "057" }, // Air France
            { "KL", "074" }, // KLM
            { "IB", "075" }, // Iberia
            { "EK", "176" } // Emirates
        };

        public TicketingService(IPaymentService paymentService, ILogger<TicketingService> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task<List<Ticket>> IssueTickets(Pnr pnr)
        {
            _logger.LogInformation("Starting ticket issuance for PNR {PnrLocator}", pnr.RecordLocator);

            // Initial validations
            if (!pnr.Fares.Any(f => f.IsStored))
                throw new InvalidOperationException("NO STORED FARE");

            if (string.IsNullOrWhiteSpace(pnr.FormOfPayment))
                throw new InvalidOperationException("NO FORM OF PAYMENT - USE FP TO ADD");

            var airSegments = pnr.Segments.Where(s => !s.IsSurfaceSegment).ToList();
            if (airSegments.Count > 4)
                throw new InvalidOperationException("MAXIMUM 4 AIR SEGMENTS PER TICKET");

            decimal totalAmount = pnr.Fares.Sum(f => f.FareAmount);

            PaymentAuthorizationResult authResult = null;

            try
            {
                // Handle credit card payments
                if (pnr.FormOfPayment.StartsWith("CC"))
                {
                    var fopParts = pnr.FormOfPayment.Split('/');

                    if (fopParts.Length != 5)
                        throw new InvalidOperationException("INVALID CREDIT CARD FOP FORMAT");

                    var cardType = fopParts[1];
                    var cardNumber = fopParts[2];
                    var cardExpiry = fopParts[3];
                    var currency = fopParts[4].Substring(0, 3);

                    // Step 1: Authorize payment
                    authResult = await _paymentService.Authorize(cardType, cardNumber, cardExpiry, totalAmount, currency, pnr.RecordLocator);

                    if (!authResult.Success)
                    {
                        throw new PaymentProcessingException(authResult.ResponseMessage);
                    }

                    _logger.LogInformation("Payment authorized for PNR {PnrLocator}: {AuthCode}",
                        pnr.RecordLocator, authResult.AuthorizationCode);
                }

                // Step 2: Create tickets
                var tickets = new List<Ticket>();

                foreach (var passenger in pnr.Passengers)
                {
                    var fare = pnr.Fares.FirstOrDefault(f => f.PassengerId == passenger.PassengerId);

                    if (fare == null)
                    {
                        _logger.LogWarning("No fare found for passenger {PassengerId} in PNR {PnrLocator}", passenger.PassengerId, pnr.RecordLocator);
                        continue;
                    }

                    var ticket = new Ticket
                    {
                        TicketNumber = GenerateTicketNumber(airSegments[0].FlightNumber.Substring(0, 2)),
                        PnrLocator = pnr.RecordLocator,
                        PassengerId = passenger.PassengerId,
                        FareAmount = fare.FareAmount,
                        Status = TicketStatus.Valid,
                        IssueDate = DateTime.UtcNow,
                        IssuingOffice = pnr.Agency.OfficeId,
                        ValidatingCarrier = fare.ValidatingCarrier,
                        Coupons = new List<Coupon>()
                    };

                    _logger.LogInformation("Generated ticket {TicketNumber} for passenger {PassengerId}", ticket.TicketNumber, passenger.PassengerId);

                    // Generate coupons only for non-ARNK segments
                    var farePerSegment = fare.FareAmount / airSegments.Count;
                    var fareBasisParts = fare.FareBasis.Split('/');
                    var couponIndex = 0;

                    // Generate coupons for each segment
                    for (int i = 0; i < pnr.Segments.Count; i++)
                    {
                        var segment = pnr.Segments[i];

                        // Skip ARNK segments
                        if (segment.IsSurfaceSegment)
                            continue;

                        var coupon = new Coupon
                        {
                            CouponNumber = (couponIndex + 1).ToString(),
                            FlightNumber = segment.FlightNumber,
                            Origin = segment.Origin,
                            Destination = segment.Destination,
                            BookingClass = segment.BookingClass,
                            FareBasis = fare.FareBasis.Split('/')[i],
                            FareAmount = farePerSegment,
                            Status = CouponStatus.Open,
                            DepartureDate = segment.DepartureDate
                        };

                        ticket.Coupons.Add(coupon);
                        couponIndex++;

                        _logger.LogDebug("Added coupon {CouponNumber} for flight {FlightNumber} to ticket {TicketNumber}", coupon.CouponNumber, coupon.FlightNumber, ticket.TicketNumber);
                    }

                    tickets.Add(ticket);
                }

                // Step 3: If credit card payment, capture the authorized amount
                if (authResult != null)
                {
                    var captureResult = await _paymentService.Capture(authResult.AuthorizationCode, totalAmount, authResult.Currency, pnr.RecordLocator);

                    if (!captureResult.Success)
                    {
                        _logger.LogError("Payment capture failed for PNR {PnrLocator}: {Message}",
                            pnr.RecordLocator, captureResult.ResponseMessage);

                        // If capture fails, reverse the authorization
                        await _paymentService.ReverseAuthorization(authResult.AuthorizationCode, pnr.RecordLocator);

                        throw new PaymentProcessingException(captureResult.ResponseMessage);
                    }

                    _logger.LogInformation("Payment captured for PNR {PnrLocator}: {CaptureCode}",
                        pnr.RecordLocator, captureResult.CaptureCode);

                    // Add payment confirmation to PNR
                    var paymentOsi = new Osi
                    {
                        Category = OsiCategory.PaymentInfo,
                        CompanyId = airSegments[0].FlightNumber.Substring(0, 2),
                        Text = $"CC PAYMENT CONFIRMED {DateTime.UtcNow:ddMMMyy/HHmm} AUTH:{authResult.AuthorizationCode} TRAN:{captureResult.TransactionId}",
                        CreatedDate = DateTime.UtcNow
                    };

                    pnr.OtherServiceInformation.Add(paymentOsi);
                }
                else
                {
                    // Add non-CC payment record
                    var paymentOsi = new Osi
                    {
                        Category = OsiCategory.PaymentInfo,
                        CompanyId = airSegments[0].FlightNumber.Substring(0, 2),
                        Text = $"PAYMENT RECORDED {DateTime.UtcNow:ddMMMyy/HHmm} FOP:{pnr.FormOfPayment}",
                        CreatedDate = DateTime.UtcNow
                    };

                    pnr.OtherServiceInformation.Add(paymentOsi);
                }

                return tickets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket issuance failed for PNR {PnrLocator}", pnr.RecordLocator);

                // If we have an authorization but haven't captured yet, reverse it
                if (authResult?.Success == true)
                {
                    try
                    {
                        await _paymentService.ReverseAuthorization(authResult.AuthorizationCode, pnr.RecordLocator);

                        _logger.LogInformation("Successfully reversed authorization {AuthCode} for PNR {PnrLocator}",
                            authResult.AuthorizationCode, pnr.RecordLocator);
                    }
                    catch (Exception reverseEx)
                    {
                        _logger.LogError(reverseEx, "Failed to reverse authorization {AuthCode} for PNR {PnrLocator}",
                            authResult.AuthorizationCode, pnr.RecordLocator);
                    }
                }

                throw new TicketingException("TICKETING FAILED - " + ex.Message, ex);
            }
        }

        public string GenerateTicketNumber(string airlineCode)
        {
            if (!_airlineNumericCodes.TryGetValue(airlineCode, out string numericCode))
                throw new ArgumentException($"Airline code {airlineCode} not supported");

            lock (_lockObj)
            {
                _ticketCounter++;

                string baseNumber = $"{numericCode}{_ticketCounter:D10}";
                string checkDigit = CalculateCheckDigit(baseNumber).ToString();

                return $"{numericCode}-{_ticketCounter:D10}{checkDigit}";
            }
        }

        public async Task<bool> VoidTicket(string ticketNumber)
        {
            _logger.LogInformation("Voiding ticket {TicketNumber}", ticketNumber);

            // Implementation would:
            // 1. Validate ticket can be voided (time limits, status, etc)
            // 2. Reverse/refund any payment
            // 3. Update ticket status
            // 4. Update coupons
            // 5. Add OSI to PNR
            throw new NotImplementedException();
        }

        public async Task<bool> RefundTicket(string ticketNumber)
        {
            _logger.LogInformation("Processing refund for ticket {TicketNumber}", ticketNumber);

            // Implementation would:
            // 1. Validate ticket can be refunded
            // 2. Calculate refund amount
            // 3. Process refund via payment service
            // 4. Update ticket status
            // 5. Update coupons
            // 6. Add OSI to PNR
            throw new NotImplementedException();
        }

        private int CalculateCheckDigit(string number)
        {
            int sum = 0;
            bool isEven = true;

            // Start from right to left, ignoring any hyphens
            for (int i = number.Length - 1; i >= 0; i--)
            {
                if (number[i] == '-') continue;

                int digit = int.Parse(number[i].ToString());

                if (isEven)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                isEven = !isEven;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit;
        }
    }
}