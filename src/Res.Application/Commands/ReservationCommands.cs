using Res.Application.Interfaces;
using Res.Core.Interfaces;
using Res.Domain.Dto;
using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;
using Res.Domain.Requests;
using Res.Domain.Responses;
using System.Globalization;
using System.Text;
using Res.Application.Parsers.Factory;
using Res.Core.Services;

namespace Res.Application.Commands
{
    public class ReservationCommands : IReservationCommands
    {
        public UserContext User { get; set; }

        private readonly IReservationService _reservationService;
        private readonly ITicketingService _ticketingService;
        private readonly IInventoryService _inventoryService;
        private readonly IFareService _fareService;
        private readonly ISpecialServiceRequestsService _specialServiceRequestsService;
        private readonly ICheckInService _checkInService;
        private readonly IApisService _apisService;
        private readonly IPaymentService _paymentService;
        private readonly ISeatService _seatService;

        private readonly ICommandParserFactory _commandParserFactory;

        private List<FlightInventory> _searchedFlights;

        public ReservationCommands(IReservationService reservationService, ITicketingService ticketingService, IInventoryService inventoryService, IFareService fareService, ISpecialServiceRequestsService specialServiceRequestsService, ICheckInService checkInService, IApisService apisService, IPaymentService paymentService, ISeatService seatService, ICommandParserFactory commandParserFactory)
        {
            _reservationService = reservationService;
            _ticketingService = ticketingService;
            _fareService = fareService;
            _specialServiceRequestsService = specialServiceRequestsService;
            _checkInService = checkInService;
            _apisService = apisService;
            _paymentService = paymentService;
            _seatService = seatService;
            _commandParserFactory = commandParserFactory;
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Load the current PNR (if there is one) based on the session ID associated with it.
        /// </summary>
        private async Task LoadCurrentSession()
        {
            _reservationService.UserContext = User;

            if (_reservationService.Pnr == null)
            {
                await _reservationService.CreatePnrWorkspace();
            }
            else
            {
                await _reservationService.LoadCurrentPnr();
            }
        }


        // TODO all the below display the same...

        public async Task<CommandResult> ProcessDisplayFareRules()
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            if (!_reservationService.Pnr.Data.Fares.Any(f => f.IsStored))
                return new CommandResult { Success = false, Message = "NO STORED FARE" };

            return new CommandResult { Success = true, Response = _reservationService.Pnr };
        }

        public async Task<CommandResult> ProcessDisplayFareHistory()
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            if (!_reservationService.Pnr.Data.Fares.Any(f => f.IsStored))
                return new CommandResult { Success = false, Message = "NO STORED FARE" };

            return new CommandResult { Success = true, Response = _reservationService.Pnr };
        }

        public async Task<CommandResult> ProcessDisplayFareNotes()
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            if (!_reservationService.Pnr.Data.Fares.Any(f => f.IsStored))
                return new CommandResult { Success = false, Message = "NO STORED FARE" };

            return new CommandResult { Success = true, Response = _reservationService.Pnr };
        }

        public async Task<CommandResult> ProcessFareQuote()
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            if (!_reservationService.Pnr.Data.Fares.Any(f => f.IsStored))
                return new CommandResult { Success = false, Message = "NO STORED FARE" };

            return new CommandResult { Success = true, Response = _reservationService.Pnr };
        }

        public async Task<CommandResult> ProcessTicketing()
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            try
            {
                var tickets = await _ticketingService.IssueTickets(_reservationService.Pnr);

                _reservationService.Pnr.Data.Tickets.AddRange(tickets);
                _reservationService.Pnr.Data.Status = PnrStatus.Ticketed;

                await _reservationService.CommitPnr();

                return new CommandResult { Success = true, Response = (tickets, _reservationService.Pnr) };

            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"UNABLE TO ISSUE TICKETS - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessAvailability(string command)
        {
            // AN20JUNLHRJFK/1400

            string departureDate = command.Substring(2, 5);  // "20JUN"
            string departureAirport = command.Substring(7, 3);  // "LHR"
            string arrivalAirport = command.Substring(10, 3);   // "JFK"

            // Get the optional time if present (after the /)
            string preferredTime = null;
            if (command.Length > 13 && command[13] == '/')
            {
                preferredTime = command.Substring(14, 4);  // "1400"
            }

            var flights = await _inventoryService.SearchAvailability(new AvailabilityRequest
            {
                DepartureDate = departureDate,
                Origin = departureAirport,
                Destination = arrivalAirport,
                PreferredTime = preferredTime
            });

            _searchedFlights = flights;

            return flights.Any() ? new CommandResult { Success = true, Response = flights } : new CommandResult { Success = false, Message = "NO FLIGHTS FOUND" };
        }

        public async Task<CommandResult> ProcessAddName(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            // Format: NM1SMITH/JOHN MR
            try
            {
                // Skip "NM1"
                var nameDetails = command.Substring(3);

                // Split surname and given names
                var parts = nameDetails.Split('/');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid name format");

                var surname = parts[0];

                // Split given name and title
                var givenNameParts = parts[1].Trim().Split(' ');
                var givenName = givenNameParts[0];
                var title = givenNameParts.Length > 1 ? givenNameParts[1] : "";

                await _reservationService.AddName(surname, givenName, title, PassengerType.Adult);

                var passenger = _reservationService.Pnr.Data.Passengers.Last();

                return new CommandResult { Success = true, Message = $"{passenger.PassengerId}. {passenger.LastName}/{passenger.FirstName} {passenger.Title}" };
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID NAME FORMAT - USE NM1SURNAME/FIRSTNAME TITLE");
            }
        }

        public async Task<CommandResult> ProcessSellSegment(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            try
            {
                FlightInventory flightToSell;
                string bookingClass;
                int quantity;

                // Parse command in the commands layer
                if (command.Length > 15) // Long sell format
                {
                    // Parse long sell format: SS VS001Y24NOVLHRJFK1
                    string flightNumber = command.Substring(3, 5).Trim(); // VS001
                    bookingClass = command.Substring(8, 1); // Y
                    string date = command.Substring(9, 5); // 24NOV
                    string from = command.Substring(14, 3); // LHR
                    string to = command.Substring(17, 3); // JFK
                    quantity = int.Parse(command.Substring(20, 1)); // 1

                    // Find flight in inventory
                    var matchingFlights = await _inventoryService.SearchAvailability(new AvailabilityRequest
                    {
                        DepartureDate = date,
                        Origin = from,
                        Destination = to
                    });

                    flightToSell = matchingFlights.FirstOrDefault(f => f.FlightNo == flightNumber);

                    if (flightToSell == null)
                        throw new ArgumentException($"Flight {flightNumber} not found");
                }
                else // Short sell format (SS1Y2)
                {
                    quantity = int.Parse(command.Substring(2, 1));
                    bookingClass = command.Substring(3, 1);
                    int lineNumber = int.Parse(command.Substring(4));

                    if (_searchedFlights == null || !_searchedFlights.Any())
                        throw new InvalidOperationException("No flights available from previous search");

                    flightToSell = _searchedFlights.ElementAtOrDefault(lineNumber - 1);

                    if (flightToSell == null)
                        throw new ArgumentException("Invalid flight selection");
                }

                // Call service with parsed data
                var segment = await _reservationService.SellSegment(flightToSell, bookingClass, quantity);

                return new CommandResult { Success = true, Response = segment };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"INVALID SELL FORMAT - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessRemoveSegment(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            try
            {
                // Format: XE1 (removes segment 1)
                int segmentNumber = int.Parse(command.Substring(2));

                await _reservationService.RemoveSegment(segmentNumber);

                return new CommandResult { Success = true, Message = $"SEGMENT {segmentNumber} REMOVED" };
            }
            catch (Exception ex)
            {
                throw new ArgumentException("INVALID SEGMENT REMOVAL FORMAT - USE XE1");
            }
        }

        public async Task<CommandResult> ProcessEndTransactionAndRecall()
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO PNR TO END TRANSACTION" };

            try
            {
                await _reservationService.CommitPnr();

                return new CommandResult
                {
                    Success = true,
                    Message = $"OK - {_reservationService.Pnr.RecordLocator}",
                    Response = _reservationService.Pnr // Return full PNR to display it
                };
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult { Success = false, Message = $"UNABLE TO END TRANSACTION - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessEndTransactionAndClear()
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO PNR TO END TRANSACTION" };

            try
            {
                await _reservationService.CommitPnr();

                string recordLocator = _reservationService.Pnr.RecordLocator;

                // Clear the PNR from context
                _reservationService.Pnr = null;

                return new CommandResult
                {
                    Success = true,
                    Message = $"OK - {recordLocator}"
                };
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult { Success = false, Message = $"UNABLE TO END TRANSACTION - {ex.Message}" };
            }
        }

        public async Task<CommandResult> AddRemark(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            string remark = command.Substring(3);

            if (string.IsNullOrWhiteSpace(remark))
                throw new ArgumentException("Remark not supplied");

            await _reservationService.AddRemarks(remark);

            return new CommandResult { Success = true, Message = "REMARK ADDED" };
        }

        public async Task<CommandResult> ProcessDisplay()
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            return _reservationService.Pnr != null ? new CommandResult { Success = true, Response = _reservationService.Pnr } : new CommandResult { Success = false, Message = "NO PNR FOUND" };
        }

        public async Task<CommandResult> ProcessDisplayPnr(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            await _reservationService.RetrievePnr(command.Substring(2));

            return _reservationService.Pnr != null ? new CommandResult { Success = true, Response = _reservationService.Pnr } : new CommandResult { Success = false, Message = "NO PNR FOUND" };
        }

        public async Task<CommandResult> ProcessDisplayAllPnrs()
        {
            var pnrs = await _reservationService.RetrieveAllPnrs();

            return pnrs.Count > 0 ? new CommandResult { Success = true, Response = pnrs } : new CommandResult { Success = false, Message = "NO PNRS FOUND" };
        }

        public async Task<CommandResult> ProcessIgnore()
        {
            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            await _reservationService.IgnoreSession();

            return new CommandResult { Success = true, Message = "IGNORED" }; ;
        }

        public async Task<CommandResult> AddTicketingArrangement(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            try
            {
                // Parse the date and optional carrier
                var parts = command.Substring(4).Split('/');

                // Parse the ticketing time limit
                var timeLimit = DateTime.ParseExact(parts[0], "ddMMM",
                    CultureInfo.InvariantCulture);

                // Get the validating carrier if provided
                string validatingCarrier = parts.Length > 1 ? parts[1] : null;

                await _reservationService.AddTicketArrangement(timeLimit, validatingCarrier);

                return new CommandResult { Success = true, Message = $"TL ADDED - {timeLimit:ddMMM}" + (validatingCarrier != null ? $" VAL {validatingCarrier}" : "") };
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID TICKETING FORMAT - USE TLTL10DEC/BA");
            }
        }

        public async Task<CommandResult> ProcessContact(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            var type = command.Substring(3, 1); // P for phone, E for email
            var value = command.Substring(5).Trim();

            switch (type)
            {
                case "P":
                    await _reservationService.AddPhone(value);
                    return new CommandResult { Success = false, Message = $"PHONE ADDED - {value}" };

                case "E":
                    await _reservationService.AddEmail(value);
                    return new CommandResult { Success = false, Message = $"EMAIL ADDED - {value}" };

                default:
                    throw new ArgumentException("INVALID CONTACT FORMAT - USE CTCP PHONE or CTCE EMAIL");
            }
        }

        public async Task<CommandResult> ProcessAgency(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            var parser = _commandParserFactory.GetParser<AgencyRequest>();
            var request = parser.Parse(command);

            await _reservationService.AddAgency(request);

            return new CommandResult { Success = true, Message = $"RECEIVED FROM {request.AgencyCode}/{request.IataNumber ?? string.Empty}/{request.AgentId ?? string.Empty}" };
        }

        public async Task<CommandResult> ProcessPricePnr(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR TO PRICE" };

            try
            {
                // Get the appropriate parser and parse the command
                var parser = _commandParserFactory.GetParser<PricePnrRequest>();

                // Delegate to the service
                _reservationService.Pnr = await _fareService.PricePnr(_reservationService.Pnr, parser.Parse(command));

                return new CommandResult { Success = true, Response = _reservationService.Pnr };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<CommandResult> ProcessStoreFare(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            if (!_reservationService.Pnr.Data.Fares.Any())
                return new CommandResult { Success = false, Message = "NO FARE TO STORE - USE FXP FIRST" };

            try
            {
                var parser = _commandParserFactory.GetParser<StoreFareRequest>();
                var request = parser.Parse(command);

                _reservationService.Pnr = await _fareService.StoreFare(_reservationService.Pnr, request);

                var message = new StringBuilder("FARE STORED");
                if (request.FareSelections.Any())
                {
                    message.Append(" - ");
                    foreach (var selection in request.FareSelections)
                    {
                        message.Append($"{selection.Key}: OPTION {selection.Value}, ");
                    }
                    message.Length -= 2; // Remove last comma and space
                }

                return new CommandResult { Success = true, Message = message.ToString().TrimEnd(',', ' ') };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<CommandResult> ProcessDisplayJson()
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            return new CommandResult { Success = true, Response = _reservationService.Pnr };
        }

        public async Task<CommandResult> ProcessAddSsr(string command)
        {
            // If no PNR is loaded into the PNR object, create a new one assigned to the user's session
            await LoadCurrentSession();

            // Format: SR WCHR/P1/S1/TXT
            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            try
            {
                // Parse command
                var parts = command.Substring(3).Split('/');
                var code = parts[0].Trim();

                int passengerId = 0;
                int segmentNumber = 0;
                string text = null;

                // Parse optional parameters
                for (int i = 1; i < parts.Length; i++)
                {
                    var part = parts[i].Trim();
                    if (part.StartsWith("P"))
                        passengerId = Convert.ToInt32(part.Substring(1));
                    else if (part.StartsWith("S"))
                        segmentNumber = Convert.ToInt32(part.Substring(1));
                    else
                        text = part;
                }

                _specialServiceRequestsService.Pnr = _reservationService.Pnr;

                await _specialServiceRequestsService.AddSsr(code, passengerId, segmentNumber, text);

                return new CommandResult { Success = true, Message = "SSR ADDED" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"UNABLE TO ADD SSR - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessDeleteSsr(string command)
        {
            // Format: SRXK1

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            try
            {
                _specialServiceRequestsService.Pnr = _reservationService.Pnr;

                await _specialServiceRequestsService.DeleteSsr(Convert.ToInt32(command.Substring(4)));

                return new CommandResult { Success = true, Message = "SSR DELETED" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"UNABLE TO DELETE SSR - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessListSsr()
        {
            // Format: SR*

            return _reservationService.Pnr != null ? new CommandResult { Success = true, Response = _reservationService.Pnr.Data.SpecialServiceRequests } : new CommandResult { Success = false, Message = "NO ACTIVE PNR" };
        }

        public async Task<CommandResult> ProcessCheckIn(string command)
        {
            // Format: CKIN ABC123/P1/VS001[/12A/B2/20.5]
            try
            {
                var parts = command.Substring(5).Split('/');
                if (parts.Length < 3)
                    throw new ArgumentException("INVALID FORMAT - USE CKIN ABC123/P1/VS001");

                string recordLocator = parts[0];
                int passengerId = int.Parse(parts[1].Substring(1));
                string flightNumber = parts[2];

                // Optional parameters
                string seatNumber = parts.Length > 3 ? parts[3] : null;
                int bagCount = 0;
                decimal bagWeight = 0;

                if (parts.Length > 5)
                {
                    bagCount = int.Parse(parts[4].TrimStart('B'));
                    bagWeight = decimal.Parse(parts[5]);
                }

                var request = new CheckInRequest
                {
                    RecordLocator = recordLocator,
                    PassengerId = passengerId,
                    FlightNumber = flightNumber,
                    SeatNumber = seatNumber,
                    HasCheckedBags = bagCount > 0,
                    BaggageCount = bagCount,
                    BaggageWeight = bagWeight,
                };

                var boardingPass = await _checkInService.CheckIn(recordLocator, request);

                return new CommandResult { Success = true, Response = boardingPass };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"CHECK-IN FAILED - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessCancelCheckIn(string command)
        {
            // Format: CXLD ABC123/P1/VS001
            try
            {
                var parts = command.Substring(5).Split('/');
                if (parts.Length != 3)
                    throw new ArgumentException("INVALID FORMAT - USE CXLD ABC123/P1/VS001");

                string recordLocator = parts[0];
                int passengerId = Convert.ToInt32(parts[1]);
                string flightNumber = parts[2];

                await _checkInService.CancelCheckIn(recordLocator, passengerId, flightNumber);

                return new CommandResult { Success = true, Message = "CHECK-IN CANCELLED" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"CANCEL CHECK-IN FAILED - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessAddDocument(string command)
        {
            // Format: SRDOCS HK1/P/GBR/P12345678/GBR/12JUL82/M/20NOV25/SMITH/JOHN
            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            try
            {
                var parts = command.Split('/');
                if (parts.Length < 9)
                    throw new ArgumentException("INVALID DOCS FORMAT");

                // Parse the document details
                var docType = parts[1];  // P for passport
                var issuingCountry = parts[2];
                var docNumber = parts[3];
                var nationality = parts[4];
                var dob = DateTime.ParseExact(parts[5], "ddMMMyyyy", CultureInfo.InvariantCulture);
                var gender = parts[6];
                var expiryDate = DateTime.ParseExact(parts[7], "ddMMMyyyy", CultureInfo.InvariantCulture);
                var lastName = parts[8];
                var firstName = parts.Length > 9 ? parts[9] : "";

                // Find matching passenger
                var passenger = _reservationService.Pnr.Data.Passengers.FirstOrDefault(p =>
                    p.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase) &&
                    p.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase));

                if (passenger == null)
                    return new CommandResult { Success = false, Message = "PASSENGER NOT FOUND" };

                // Create new document
                var document = new Document
                {
                    Type = docType,
                    Number = docNumber,
                    IssuingCountry = issuingCountry,
                    Nationality = nationality,
                    DateOfBirth = dob,
                    Gender = gender,
                    ExpiryDate = expiryDate
                };

                // Add document to passenger
                passenger.Documents.Add(document);

                // Add as SSR
                var ssrText = $"HK1/P/{issuingCountry}/{docNumber}/{nationality}/{dob:ddMMMyy}/{gender}/{expiryDate:ddMMMyy}/{lastName}/{firstName}";
                await _specialServiceRequestsService.AddSsr("DOCS", passenger.PassengerId, 0, ssrText);

                return new CommandResult { Success = true, Message = "DOCUMENT ADDED" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"ERROR ADDING DOCUMENT - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessDeleteDocument(string command)
        {
            // Format: SRXD1 - Delete document number 1
            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            try
            {
                // Extract document index from command
                var docIndex = int.Parse(command.Substring(4));

                // Since documents are stored per passenger, we need to find which passenger
                // has this document and remove it
                foreach (var passenger in _reservationService.Pnr.Data.Passengers)
                {
                    if (docIndex <= passenger.Documents.Count)
                    {
                        var document = passenger.Documents[docIndex - 1];
                        passenger.Documents.RemoveAt(docIndex - 1);

                        // Also remove corresponding DOCS SSR
                        var docsSsr = _reservationService.Pnr.Data.SpecialServiceRequests
                            .FirstOrDefault(ssr =>
                                ssr.Type == SsrType.Passport &&
                                ssr.PassengerId == passenger.PassengerId &&
                                ssr.Text.Contains(document.Number));

                        if (docsSsr != null)
                        {
                            _reservationService.Pnr.Data.SpecialServiceRequests.Remove(docsSsr);
                        }

                        return new CommandResult { Success = true, Message = $"DOCUMENT {docIndex} DELETED" };
                    }
                    docIndex -= passenger.Documents.Count;
                }

                return new CommandResult { Success = false, Message = "DOCUMENT NOT FOUND" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"ERROR DELETING DOCUMENT - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessListDocuments()
        {
            return _reservationService.Pnr != null ? new CommandResult { Success = true, Response = _reservationService.Pnr } : new CommandResult { Success = false, Message = "NO PNR FOUND" };
        }

        public async Task<CommandResult> ProcessCheckInAll(string command)
        {
            // Format: CKINALL ABC123/VS123
            try
            {
                var parts = command.Substring(8).Split('/');

                if (parts.Length != 2)
                    throw new ArgumentException("INVALID FORMAT - USE CKINALL ABC123/VS123");

                string recordLocator = parts[0];
                string flightNumber = parts[1];

                var boardingPasses = await _checkInService.CheckInAll(recordLocator, flightNumber);

                return new CommandResult { Success = true, Response = boardingPasses };
            }
            catch (AggregateException aex)
            {
                var errorMessages = aex.InnerExceptions.Select(ex => ex.Message);

                return new CommandResult { Success = false, Message = $"PARTIAL CHECK-IN COMPLETED WITH ERRORS:\n{string.Join("\n", errorMessages)}" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"CHECK-IN ALL FAILED - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessTicketListDisplay(string command)
        {
            // TKTL - List all tickets in PNR
            return _reservationService.Pnr != null ? new CommandResult { Success = true, Response = _reservationService.Pnr } : new CommandResult { Success = false, Message = "NO ACTIVE PNR" };
        }

        public async Task<CommandResult> ProcessTicketDisplay(string command)
        {
            // TKT/9321234567890 - Display specific ticket

            if (_reservationService.Pnr == null)
                new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            // Extract ticket number from command (TKT/9321234567890)

            var parts = command.Split('/');

            if (parts.Length != 2)
                throw new InvalidOperationException("Invalid TKT parameters");

            if (parts[0].Length != 3)
                throw new InvalidOperationException("Invalid TKT command");

            string ticketNumber = parts[1];
            var ticket = _reservationService.Pnr.Data.Tickets.FirstOrDefault(t => t.TicketNumber == ticketNumber);

            return new CommandResult { Success = true, Response = (ticket, _reservationService.Pnr) };
        }

        public async Task<CommandResult> ProcessRetrieveByName(string command)
        {
            // Format: RTNSMITH/JOHN
            try
            {
                string nameData = command.Substring(3);
                var parts = nameData.Split('/');
                string lastName = parts[0];
                string firstName = parts.Length > 1 ? parts[1] : null;

                var pnrs = await _reservationService.RetrieveByName(lastName, firstName);

                return new CommandResult { Success = true, Response = pnrs };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = "INVALID NAME FORMAT" };
            }
        }

        public async Task<CommandResult> ProcessRetrieveByFlight(string command)
        {
            // Format: RTVS001/24NOV
            try
            {
                string data = command.Substring(2);
                var parts = data.Split('/');
                string flightNumber = parts[0];
                string date = parts[1];

                var pnrs = await _reservationService.RetrieveByFlight(flightNumber, date);

                return new CommandResult { Success = true, Response = pnrs };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = "INVALID FLIGHT FORMAT" };
            }
        }

        public async Task<CommandResult> ProcessRetrieveByPhone(string command)
        {
            // Format: RTCT442071234567
            try
            {
                string phoneNumber = command.Substring(4);
                var pnrs = await _reservationService.RetrieveByPhone(phoneNumber);

                return new CommandResult { Success = true, Response = pnrs };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = "INVALID PHONE FORMAT" };
            }
        }

        public async Task<CommandResult> ProcessRetrieveByTicket(string command)
        {
            // Format: RTTK9321234567890
            try
            {
                string ticketNumber = command.Substring(4);
                var pnrs = await _reservationService.RetrieveByTicket(ticketNumber);

                return new CommandResult { Success = true, Response = pnrs };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = "INVALID TICKET FORMAT" };
            }
        }

        public async Task<CommandResult> ProcessRetrieveByFrequentFlyer(string command)
        {
            // Format: RTFF12345678
            try
            {
                string ffNumber = command.Substring(4);
                var pnrs = await _reservationService.RetrieveByFrequentFlyer(ffNumber);

                return new CommandResult { Success = true, Response = pnrs };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = "INVALID FF NUMBER FORMAT" };
            }
        }

        public async Task<CommandResult> ProcessDisplayApis(string command)
        {
            // Format: APIS ABC123/VS001
            try
            {
                var parts = command.Substring(5).Split('/');
                string recordLocator = parts[0];
                string flightNumber = parts[1];

                var apisData = await _apisService.BuildApisFromPnr(recordLocator, flightNumber);

                return new CommandResult { Success = true, Response = apisData };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<CommandResult> ProcessAddApiAddress(string command)
        {
            // Format: SRDOCA HK1/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA
            try
            {
                if (_reservationService.Pnr == null)
                    return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

                var parts = command.Substring(6).Split('/');
                if (parts.Length < 7)
                    throw new ArgumentException("INVALID ADDRESS FORMAT");

                var action = parts[0];     // HK1
                var paxNum = parts[1];     // P1
                var type = parts[2];       // R for residence
                var country = parts[3];    // GBR
                var street = parts[4];     // 123 HIGH STREET
                var city = parts[5];       // LONDON
                var state = parts[6];      // GB
                var postal = parts[7];     // W1A 1AA

                if (type != "R" && type != "D")
                    throw new ArgumentException("ADDRESS TYPE MUST BE R (RESIDENCE) OR D (DESTINATION)");

                var passengerId = int.Parse(paxNum.Substring(1));

                var ssrText = $"{action}/{paxNum}/{type}/{country}/{street}/{city}/{state}/{postal}";

                _specialServiceRequestsService.Pnr = _reservationService.Pnr;
                await _specialServiceRequestsService.AddSsr("DOCA", passengerId, 0, ssrText);

                var addressType = type == "R" ? "RESIDENCE" : "DESTINATION";
                return new CommandResult { Success = true, Message = $"{addressType} ADDRESS ADDED" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"ERROR ADDING ADDRESS - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessFormOfPayment(string command)
        {
            // FP*CC/VISA/4444333322221111/0625/GBP892.00 - Credit card
            // FP*CA/GBP892.00 - Cash
            // FP*MS/INVOICE/GBP892.00 - Miscellaneous

            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            try
            {
                string fop = command.Substring(3); // Remove FP*

                // Validate FOP format based on type
                if (fop.StartsWith("CC/"))
                {
                    // Credit card format validation
                    var parts = fop.Split('/');
                    if (parts.Length != 5)
                        throw new ArgumentException("INVALID CREDIT CARD FORMAT - USE CC/TYPE/NUMBER/EXPIRY/AMOUNT");

                    // Use payment service to validate card
                    if (!_paymentService.ValidateCard(parts[2], parts[3]))
                        return new CommandResult { Success = false, Message = "INVALID CARD" };
                }
                else if (fop.StartsWith("CA/"))
                {
                    // Cash format validation
                    var parts = fop.Split('/');
                    if (parts.Length != 2)
                        throw new ArgumentException("INVALID CASH FORMAT - USE CA/AMOUNT");
                }
                else if (fop.StartsWith("MS/"))
                {
                    // Misc format validation
                    var parts = fop.Split('/');
                    if (parts.Length != 3)
                        throw new ArgumentException("INVALID MISC FORMAT - USE MS/REFERENCE/AMOUNT");
                }
                else
                {
                    throw new ArgumentException("INVALID FOP TYPE - USE CC, CA, or MS");
                }

                _reservationService.Pnr = await _fareService.AddFormOfPayment(_reservationService.Pnr, fop);

                return new CommandResult { Success = true, Message = "FORM OF PAYMENT ADDED" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"INVALID FOP FORMAT - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessDisplaySeatMap(string command)
        {
            try
            {
                if (_reservationService.Pnr == null && !command.Contains("/"))
                    return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

                string flightNumber;
                string departureDate;
                string bookingClass = null;

                if (command.Length == 3) // SM1 format
                {
                    int segmentNumber = int.Parse(command.Substring(2, 1));
                    var segment = _reservationService.Pnr.Data.Segments[segmentNumber - 1];
                    flightNumber = segment.FlightNumber;
                    departureDate = segment.DepartureDate;
                    bookingClass = segment.BookingClass;
                }
                else // SM VS123/12MAY format
                {
                    var parts = command.Substring(3).Split('/');
                    flightNumber = parts[0];
                    departureDate = parts[1];
                }

                var seatMap = await _seatService.DisplaySeatMap(flightNumber, departureDate, bookingClass);

                if (seatMap == null)
                    return new CommandResult { Success = false, Message = "FLIGHT NOT FOUND" };

                return new CommandResult { Success = true, Response = seatMap };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"ERROR DISPLAYING SEAT MAP: {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessAssignSeat(string command)
        {
            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            try
            {
                var parts = command.Substring(3).Split('/');
                if (parts.Length != 3)
                    throw new ArgumentException("INVALID FORMAT - USE ST/4D/P1/S1");

                string seatNumber = parts[0];
                int passengerId = Convert.ToInt32(parts[1].Substring(1)); // Remove 'P'
                string segmentNumber = parts[2].Substring(1); // Remove 'S'

                _seatService.Pnr = _reservationService.Pnr;

                await _seatService.AssignSeat(seatNumber, passengerId, segmentNumber);
                await _reservationService.CommitPnr();

                return new CommandResult { Success = true, Message = $"SEAT {seatNumber} ASSIGNED" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"UNABLE TO ASSIGN SEAT - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessRemoveSeat(string command)
        {
            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            try
            {
                var parts = command.Substring(4).Split('/');
                if (parts.Length != 2)
                    throw new ArgumentException("INVALID FORMAT - USE STX/P1/S1");

                int passengerId = Convert.ToInt32(parts[0].Substring(1)); // Remove 'P'
                string segmentNumber = parts[1].Substring(1); // Remove 'S'

                await _seatService.RemoveSeat(passengerId, segmentNumber);
                await _reservationService.CommitPnr();

                return new CommandResult { Success = true, Message = "SEAT ASSIGNMENT REMOVED" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"UNABLE TO REMOVE SEAT - {ex.Message}" };
            }
        }

        public async Task<CommandResult> ProcessArnk(string command)
        {
            if (_reservationService.Pnr == null)
                return new CommandResult { Success = false, Message = "NO ACTIVE PNR" };

            // TODO add support to add an ARNK after a particular segment - extract the position from the command, e.g. ARNK/2 adds it after the first segment

            await _reservationService.AddArnkSegment(null);

            return new CommandResult
            {
                Success = true,
                Message = "SURFACE SEGMENT ADDED"
            };
        }

        public async Task<CommandResult> ProcessDeletePnr(string command)
        {
            // Format: XI ABC123
            try
            {
                string recordLocator = command.Substring(3);

                await _reservationService.DeletePnr(recordLocator);

                return new CommandResult { Success = true, Message = $"PNR {recordLocator} DELETED" };
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = $"ERROR DELETING PNR - {ex.Message}" };
            }
        }

        // more operations added above this line

        public async Task<string> GetHelpText()
        {
            var sb = new StringBuilder();

            sb.AppendLine("AVAILABLE COMMANDS");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine("");

            sb.AppendLine("PNR RETRIEVAL");
            sb.AppendLine("  RTABCDEF                 - Retrieve PNR by record locator");
            sb.AppendLine("  RTALL                    - List all PNRs");
            sb.AppendLine("  RTNSMITH/JOHN            - Retrieve by passenger name");
            sb.AppendLine("  RTVS001/24NOV            - Retrieve by flight number/date");
            sb.AppendLine("  RTCT442071234567         - Retrieve by phone number");
            sb.AppendLine("  RTTK9321234567890        - Retrieve by ticket number");
            sb.AppendLine("  RTFF12345678             - Retrieve by frequent flyer number");
            sb.AppendLine("");

            sb.AppendLine("PNR COMMANDS");
            sb.AppendLine("  AN24NOVLHRJFK/1400       - Search flights (date/origin/destination/optional time)");
            sb.AppendLine("  SS1Y2                    - Sell seat (quantity/class/line number from availability)");
            sb.AppendLine("  ARNK                     - Add surface segment (non-air travel)");
            sb.AppendLine("  XI ABC123                - Delete PNR (releases inventory and seats)");
            sb.AppendLine("  XE1                      - Removes segment specified from the current PNR");
            sb.AppendLine("  SS VS001Y24NOVLHRJFK1    - Long sell (flight/class/date/origin/dest/quantity)");
            sb.AppendLine("  NM1SMITH/JOHN MR         - Add passenger name (surname/firstname title)");
            sb.AppendLine("  CTCP 44123456789         - Add phone contact");
            sb.AppendLine("  CTCE JOHN@EMAIL.COM      - Add email contact");
            sb.AppendLine("  RF ABC/12345678/JAGENT   - Add agency details (code/iata/agent)");
            sb.AppendLine("  TLTL24NOV/VS             - Add ticketing time limit and carrier");
            sb.AppendLine("  RM FREQUENT FLYER 123    - Add a remark");
            sb.AppendLine("  TTP                      - Issues tickets for all passengers in the PNRs");
            sb.AppendLine("  TKTL                     - List all tickets in PNR");
            sb.AppendLine("  TKT/NUMBER               - Display specific ticket details");
            sb.AppendLine("  RTAL                     - Return all PNRs");
            sb.AppendLine("  RTABCDEFG                - Search a PNR by record locator");
            sb.AppendLine("  *R                       - Display current PNR");
            sb.AppendLine("  ER                       - Save and end transaction");
            sb.AppendLine("  IG                       - Cancel current transaction");

            sb.AppendLine("");
            sb.AppendLine("SSR COMMANDS");
            sb.AppendLine("  SR WCHR/P1/S1/TXT        - Add SSR (code/pax/segment/text)");
            sb.AppendLine("  SRXK1                    - Delete SSR by ID");
            sb.AppendLine("  SR*                      - List all SSRs");

            sb.AppendLine("");
            sb.AppendLine("\nFARE COMMANDS");
            sb.AppendLine("  FXP                      - Price PNR showing all fare family options");
            sb.AppendLine("  FXP/R                    - Reprice PNR");
            sb.AppendLine("  FXP/R,FC-USD             - Price in USD currency");
            sb.AppendLine("  FXP/R,FC-EUR             - Price in EUR currency");
            sb.AppendLine("  FXP/R,FC-GBP             - Price in GBP currency");
            sb.AppendLine("");
            sb.AppendLine("  FS                       - Store cheapest fare family for all passengers");
            sb.AppendLine("  FS1                      - Store fare family option 1 for all passengers");
            sb.AppendLine("  FS2                      - Store fare family option 2 for all passengers");
            sb.AppendLine("  FS3                      - Store fare family option 3 for all passengers");
            sb.AppendLine("  FS1,2                    - Store option 1 for adults, 2 for children");
            sb.AppendLine("  FS1,2,3                  - Store option 1 for adults, 2 for children, 3 for infants");
            sb.AppendLine("");
            sb.AppendLine("  FARE FAMILIES:");
            sb.AppendLine("    Basic                  - Non-refundable, Non-changeable, Basic benefits");
            sb.AppendLine("    Classic                - Changeable with fee, Standard benefits");
            sb.AppendLine("    Flex                   - Fully flexible, Premium benefits");
            sb.AppendLine("  FP*CC/VISA/4444333322221111/0625/GBP892.00 - Add form of payment (card type/card number/exp/amount)");
            sb.AppendLine("  FP* CA/ GBP892.00        -Add form of payment (cash)");
            sb.AppendLine("  FP* MS/ INVOICE / GBP892.00   -Add form of payment (misc)");
            sb.AppendLine("  FQD                      - Display fare quote details");
            sb.AppendLine("  FN                       - Display fare notes and rules");
            sb.AppendLine("  FH                       - Display fare history");
            sb.AppendLine("  FV*                      - Display detailed fare rules");

            sb.AppendLine("");
            sb.AppendLine("SEAT MAP COMMANDS");
            sb.AppendLine("  SM1                      - Display seat map for segment 1 of current PNR");
            sb.AppendLine("  SM VS123/12MAY           - Display seat map for specific flight/date");
            sb.AppendLine("  ST/4D/P1/S1              - Assign seat (seat/passenger/segment)");
            sb.AppendLine("  STX/P1/S1                - Remove seat assignment (passenger/segment)");
            sb.AppendLine("  Legend:");
            sb.AppendLine("    . = Available          - Regular available seat");
            sb.AppendLine("    X = Occupied           - Seat is already assigned");
            sb.AppendLine("    * = Blocked            - Seat blocked for operational reasons");
            sb.AppendLine("    E = Exit Row           - Emergency exit row seat");
            sb.AppendLine("    B = Bulkhead           - Bulkhead row seat");

            sb.AppendLine("DOCUMENT COMMANDS");
            sb.AppendLine("  SRDOCS HK1/P/GBR/P12345678/GBR/12JUL82/M/20NOV25/SMITH/JOHN");
            sb.AppendLine("       - Add passport (Type/Country/Number/Nationality/DOB/Gender/Expiry/Name)");
            sb.AppendLine("  SRXD1 - Delete document 1");
            sb.AppendLine("  DOCS* - List all documents");

            sb.AppendLine("");
            sb.AppendLine("CHECK-IN COMMANDS");
            sb.AppendLine("  CKIN ABC123/P1/VS001/12A     - Check in (PNR/Pax/Flight/Seat)");
            sb.AppendLine("  CKIN ABC123/P1/VS001/12A/B2/20.5 - With bags (Count/Weight)");
            sb.AppendLine("  CXLD ABC123/P1/VS001         - Cancel check-in");
            sb.AppendLine("  PRNT ABC123/P1/VS001         - Reprint boarding pass");

            sb.AppendLine("");
            sb.AppendLine("APIS COMMANDS");
            sb.AppendLine("  APIS ABC123/VS001         - Display APIS data for PNR/flight");
            sb.AppendLine("  SRDOCA HK1/P1/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA   - Add address for APIS (HK1/passenger/type/country/...)");
            sb.AppendLine("  R = Residence address (where passenger lives)");
            sb.AppendLine("  D = Destination address (where staying during travel)");
            sb.AppendLine("  SRDOCS HK1/P/GBR/...     - Add passport for APIS");

            sb.AppendLine("");
            sb.AppendLine("MISC COMMANDS");
            sb.AppendLine("  CLEAR                    - Clear screen");
            sb.AppendLine("  EXIT                     - Exit application");
            sb.AppendLine("  HELP                     - Show this help");

            return sb.ToString();
        }
    }
}