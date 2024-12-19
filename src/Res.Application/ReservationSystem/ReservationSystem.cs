using Res.Application.Extensions;
using Res.Application.Interfaces;
using Res.Core.Interfaces;
using Res.Domain.Dto;
using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.Pnr;
using Res.Domain.Entities.SeatMap;
using Res.Domain.Entities.Ticket;
using Res.Domain.Enums;

namespace Res.Application.ReservationSystem
{
    public class ReservationSystem : IReservationSystem
    {
        private readonly IReservationCommands _reservationCommands;
        private readonly IUserService _userService;

        public ReservationSystem(IReservationCommands reservationCommands, IUserService userService)
        {
            _reservationCommands = reservationCommands;
            _userService = userService;
        }

        public async Task<CommandResult> ProcessCommand(string crypticCommand, string token)
        {
            // Validate token first
            var tokenValidationResponse = _userService.ValidateToken(token);

            if (!tokenValidationResponse.Success)
            {
                return new CommandResult
                {
                    Success = false,
                    Response = "SESSION EXPIRED - PLEASE LOGIN AGAIN"
                };
            }

            _reservationCommands.User = tokenValidationResponse.UserContext;

            crypticCommand = crypticCommand.Trim().ToUpper();

            string output;
            CommandResult commandResult;

            //try
            //{
            // Parse the command type
            var commandType = DetermineCommandType(crypticCommand);

            switch (commandType)
            {
                case CommandType.Availability:

                    // Example: AN20JUNLHRJFK/1400
                    commandResult = await _reservationCommands.ProcessAvailability(crypticCommand);

                    output = commandResult.Success ? ((List<FlightInventory>)commandResult.Response).OutputSearchResults() : commandResult.Message;

                    break;

                case CommandType.AddName:
                    // NM1SMITH/JOHN MR

                    commandResult = await _reservationCommands.ProcessAddName(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.Remark:
                    // RM FREQUENT FLYER VS213334

                    commandResult = await _reservationCommands.AddRemark(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.SellSegment:
                    // SS1Y2

                    commandResult = await _reservationCommands.ProcessSellSegment(crypticCommand);

                    output = commandResult.Success ? ((Segment)commandResult.Response).OutputAddSegment() : commandResult.Message;

                    break;

                case CommandType.RemoveSegment:
                    // XE1

                    commandResult = await _reservationCommands.ProcessRemoveSegment(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.ArrivalUnknown:

                    commandResult = await _reservationCommands.ProcessArnk(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.EndTransaction:
                    // ER - End transaction and recall
                    commandResult = await _reservationCommands.ProcessEndTransactionAndRecall();
                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputPnr() : commandResult.Message;
                    break;

                case CommandType.EndTransactionClear:
                    // ET - End transaction and clear
                    commandResult = await _reservationCommands.ProcessEndTransactionAndClear();
                    output = commandResult.Message;
                    break;

                case CommandType.DeletePnr:
                    commandResult = await _reservationCommands.ProcessDeletePnr(crypticCommand);
                    output = commandResult.Message;
                    break;

                case CommandType.Display:
                    // *R

                    commandResult = await _reservationCommands.ProcessDisplay();

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputPnr() : commandResult.Message;

                    break;

                case CommandType.DisplayAllPnr:
                    // RTALL

                    commandResult = await _reservationCommands.ProcessDisplayAllPnrs();

                    output = commandResult.Success ? ((List<Pnr>)commandResult.Response).OutputPnrs() : commandResult.Message;

                    break;

                case CommandType.DisplayPnr:
                    // RTABCDEFG
                    commandResult = await _reservationCommands.ProcessDisplayPnr(crypticCommand);

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputPnr() : commandResult.Message;

                    break;

                case CommandType.RetrieveByName:
                    commandResult = await _reservationCommands.ProcessRetrieveByName(crypticCommand);
                    output = commandResult.Success ? ((List<Pnr>)commandResult.Response).OutputPnrs() : commandResult.Message;
                    break;

                case CommandType.RetrieveByFlight:
                    commandResult = await _reservationCommands.ProcessRetrieveByFlight(crypticCommand);
                    output = commandResult.Success ? ((List<Pnr>)commandResult.Response).OutputPnrs() : commandResult.Message;
                    break;

                case CommandType.RetrieveByPhone:
                    commandResult = await _reservationCommands.ProcessRetrieveByPhone(crypticCommand);
                    output = commandResult.Success ? ((List<Pnr>)commandResult.Response).OutputPnrs() : commandResult.Message;
                    break;

                case CommandType.RetrieveByTicket:
                    commandResult = await _reservationCommands.ProcessRetrieveByTicket(crypticCommand);
                    output = commandResult.Success ? ((List<Pnr>)commandResult.Response).OutputPnrs() : commandResult.Message;
                    break;

                case CommandType.RetrieveByFrequentFlyer:
                    commandResult = await _reservationCommands.ProcessRetrieveByFrequentFlyer(crypticCommand);
                    output = commandResult.Success ? ((List<Pnr>)commandResult.Response).OutputPnrs() : commandResult.Message;
                    break;

                case CommandType.Ignore:
                    // IG

                    commandResult = await _reservationCommands.ProcessIgnore();

                    output = commandResult.Message;

                    break;

                case CommandType.AddTicketingArrangement:
                    // TLTL10DEC/BA

                    commandResult = await _reservationCommands.AddTicketingArrangement(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.AddContact:
                    // CTCP 44123456789
                    // CTCE TEST@EMAIL.COM

                    commandResult = await _reservationCommands.ProcessContact(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.AddAgency:
                    // AGY ABC/12345678/JAGENT

                    commandResult = await _reservationCommands.ProcessAgency(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.AddSsr:

                    commandResult = await _reservationCommands.ProcessAddSsr(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.DeleteSsr:

                    commandResult = await _reservationCommands.ProcessDeleteSsr(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.ListSsr:

                    commandResult = await _reservationCommands.ProcessListSsr();

                    output = commandResult.Success ? ((List<Ssr>)commandResult.Response).OutputSsrList() : commandResult.Message;

                    break;

                case CommandType.PricePnr:

                    commandResult = await _reservationCommands.ProcessPricePnr(crypticCommand);

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputFareInfo() : commandResult.Message;

                    break;

                case CommandType.StoreFare:

                    commandResult = await _reservationCommands.ProcessStoreFare(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.FormOfPayment:

                    commandResult = await _reservationCommands.ProcessFormOfPayment(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.DisplayFareQuote:
                    // FXP

                    commandResult = await _reservationCommands.ProcessFareQuote();

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputFareQuote() : commandResult.Message;

                    break;

                case CommandType.DisplayFareNotes:
                    // FN

                    commandResult = await _reservationCommands.ProcessDisplayFareNotes();

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputFareNotes() : commandResult.Message;

                    break;

                case CommandType.DisplayFareHistory:
                    // FH

                    commandResult = await _reservationCommands.ProcessDisplayFareHistory();

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputFareHistory() : commandResult.Message;

                    break;

                case CommandType.DisplayFareRules:
                    // FV*

                    commandResult = await _reservationCommands.ProcessDisplayFareRules();

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputFareRules() : commandResult.Message;

                    break;

                case CommandType.IssueTicket:
                    // TTP - Ticket PNR

                    commandResult = await _reservationCommands.ProcessTicketing();

                    output = commandResult.Success ? (((List<Ticket>, Pnr))commandResult.Response).Item1.OutputTickets((((List<Ticket>, Pnr))commandResult.Response).Item2) : commandResult.Message;

                    break;

                case CommandType.TicketListDisplay:
                    // TKTL - List all tickets in PNR

                    commandResult = await _reservationCommands.ProcessTicketListDisplay(crypticCommand);

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputTickets() : commandResult.Message;

                    break;

                case CommandType.TicketDisplay:
                    //TKT/123-8346587346

                    commandResult = await _reservationCommands.ProcessTicketDisplay(crypticCommand);

                    var response = ((Ticket, Pnr))commandResult.Response;

                    output = commandResult.Success ? response.Item1.OutputTicket(response.Item2) : commandResult.Message;

                    break;

                case CommandType.Json:
                    // *J

                    commandResult = await _reservationCommands.ProcessDisplayJson();

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputJson() : commandResult.Message;

                    break;

                case CommandType.CheckIn:

                    commandResult = await _reservationCommands.ProcessCheckIn(crypticCommand);

                    output = commandResult.Success ? ((BoardingPass)commandResult.Response).OutputBoardingPass() : commandResult.Message;

                    break;

                case CommandType.CheckInAll:

                    commandResult = await _reservationCommands.ProcessCheckInAll(crypticCommand);

                    output = commandResult.Success ? ((List<BoardingPass>)commandResult.Response).OutputBoardingPasses() : commandResult.Message;

                    break;

                case CommandType.CancelCheckIn:
                    // CXLD ABC123/P1/VS001

                    commandResult = await _reservationCommands.ProcessCancelCheckIn(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.PrintBoardingPass:
                    // Implementation similar to check-in but only generates boarding pass

                    commandResult = await _reservationCommands.ProcessCheckIn(crypticCommand.Replace("PRNT", "CKIN"));

                    output = commandResult.Success ? ((BoardingPass)commandResult.Response).OutputBoardingPass() : commandResult.Message;

                    break;

                case CommandType.AddDocument:

                    commandResult = await _reservationCommands.ProcessAddDocument(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.DeleteDocument:

                    commandResult = await _reservationCommands.ProcessDeleteDocument(crypticCommand);

                    output = commandResult.Message;

                    break;

                case CommandType.ListDocuments:

                    commandResult = await _reservationCommands.ProcessListDocuments();

                    output = commandResult.Success ? ((Pnr)commandResult.Response).OutputDocs() : commandResult.Message;

                    break;

                case CommandType.DisplayApis:
                    // APIS ABC123/VS001
                    commandResult = await _reservationCommands.ProcessDisplayApis(crypticCommand);
                    output = ((ApisData)commandResult.Response).OutputApisData();
                    break;

                case CommandType.AddApiAddress:
                    // SRDOCA HK1/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA
                    commandResult = await _reservationCommands.ProcessAddApiAddress(crypticCommand);
                    output = commandResult.Message;
                    break;

                case CommandType.DisplaySeatMap:
                    commandResult = await _reservationCommands.ProcessDisplaySeatMap(crypticCommand);
                    output = commandResult.Success ? ((SeatMap)commandResult.Response).OutputSeatMap() : commandResult.Message;
                    break;

                case CommandType.AssignSeat:
                    commandResult = await _reservationCommands.ProcessAssignSeat(crypticCommand);
                    output = commandResult.Message;
                    break;

                case CommandType.RemoveSeat:
                    commandResult = await _reservationCommands.ProcessRemoveSeat(crypticCommand);
                    output = commandResult.Message;
                    break;

                case CommandType.Help:
                    // HE
                    output = await _reservationCommands.GetHelpText();
                    break;

                default:
                    return new CommandResult
                    {
                        Success = false,
                        Response = "INVALID COMMAND",
                    };
            }
            //}
            //catch (Exception ex)
            //{
            //    return new CommandResult
            //    {
            //        Success = false,
            //        Response = $"ERROR - {ex.Message}"
            //    };
            //}

            return new CommandResult
            {
                Success = true,
                Response = output
            };
        }

        private CommandType DetermineCommandType(string command)
        {
            if (command.StartsWith("AN")) return CommandType.Availability;
            if (command.StartsWith("SS")) return CommandType.SellSegment;
            if (command == "ARNK") return CommandType.ArrivalUnknown;
            if (command.StartsWith("XE")) return CommandType.RemoveSegment;
            if (command.StartsWith("NM")) return CommandType.AddName;
            if (command == "ER") return CommandType.EndTransaction;
            if (command == "ET") return CommandType.EndTransactionClear;
            if (command == "*R") return CommandType.Display;
            if (command == "RTALL") return CommandType.DisplayAllPnr;
            if (command.StartsWith("RT") && command.Length == 8) return CommandType.DisplayPnr;
            if (command.StartsWith("RTN")) return CommandType.RetrieveByName;
            if (command.StartsWith("RTV")) return CommandType.RetrieveByFlight;
            if (command.StartsWith("RTCT")) return CommandType.RetrieveByPhone;
            if (command.StartsWith("RTTK")) return CommandType.RetrieveByTicket;
            if (command.StartsWith("RTFF")) return CommandType.RetrieveByFrequentFlyer;
            if (command.StartsWith("XI")) return CommandType.DeletePnr;

            if (command.StartsWith("TLTL")) return CommandType.AddTicketingArrangement;
            if (command.StartsWith("CTC")) return CommandType.AddContact;
            if (command.StartsWith("RF")) return CommandType.AddAgency;
            if (command.StartsWith("RM")) return CommandType.Remark;
            if (command.StartsWith("SR ")) return CommandType.AddSsr;
            if (command.StartsWith("SRX")) return CommandType.DeleteSsr;
            if (command == "SR*") return CommandType.ListSsr;

            if (command.StartsWith("SM")) return CommandType.DisplaySeatMap;
            if (command.StartsWith("ST/")) return CommandType.AssignSeat;
            if (command.StartsWith("STX/")) return CommandType.RemoveSeat;

            if (command.StartsWith("FXP")) return CommandType.PricePnr;
            if (command.StartsWith("FS")) return CommandType.StoreFare;
            if (command.StartsWith("FP")) return CommandType.FormOfPayment;
            if (command.StartsWith("FQD") || command.StartsWith("FQQ")) return CommandType.DisplayFareQuote;
            if (command.StartsWith("FN")) return CommandType.DisplayFareNotes;
            if (command.StartsWith("FH")) return CommandType.DisplayFareHistory;
            if (command.StartsWith("FV*")) return CommandType.DisplayFareRules;
            if (command.StartsWith("TTP")) return CommandType.IssueTicket;
            if (command == "TKTL") return CommandType.TicketListDisplay;
            if (command.StartsWith("TKT")) return CommandType.TicketDisplay;

            if (command.StartsWith("CKIN")) return CommandType.CheckIn;
            if (command.StartsWith("CXLD")) return CommandType.CancelCheckIn;
            if (command.StartsWith("CKINALL")) return CommandType.CheckInAll;
            if (command.StartsWith("PRNT")) return CommandType.PrintBoardingPass;

            if (command.StartsWith("SRDOCS")) return CommandType.AddDocument;
            if (command.StartsWith("SRXD")) return CommandType.DeleteDocument;
            if (command == "DOCS*") return CommandType.ListDocuments;

            if (command == "*J") return CommandType.Json;
            if (command.StartsWith("XE")) return CommandType.Cancel;
            if (command.StartsWith("IG")) return CommandType.Ignore;
            if (command.StartsWith("HE")) return CommandType.Help;

            if (command.StartsWith("APIS")) return CommandType.DisplayApis;
            if (command.StartsWith("SRDOCA")) return CommandType.AddApiAddress;

            throw new ArgumentException("Invalid command format");
        }
    }
}