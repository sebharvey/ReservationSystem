using Res.Domain.Dto;
using Res.Domain.Responses;

namespace Res.Application.Interfaces
{
    public interface IReservationCommands
    {
        Task<CommandResult> ProcessDisplayFareRules();
        Task<CommandResult> ProcessDisplayFareHistory();
        Task<CommandResult> ProcessDisplayFareNotes();
        Task<CommandResult> ProcessFareQuote();
        Task<CommandResult> ProcessTicketing();
        Task<CommandResult> AddTicketingArrangement(string command);
        Task<CommandResult> ProcessAvailability(string command);
        Task<CommandResult> ProcessAddName(string command);
        Task<CommandResult> ProcessSellSegment(string command);
        Task<CommandResult> ProcessRemoveSegment(string command);
        Task<CommandResult> ProcessEndTransactionAndRecall();
        Task<CommandResult> ProcessEndTransactionAndClear();
        Task<CommandResult> AddRemark(string command);
        Task<CommandResult> ProcessDisplay();
        Task<CommandResult> ProcessDisplayPnr(string command);
        Task<CommandResult> ProcessDisplayAllPnrs();
        Task<CommandResult> ProcessIgnore();
        Task<CommandResult> ProcessContact(string command);
        Task<CommandResult> ProcessAgency(string command);
        Task<CommandResult> ProcessPricePnr(string command);
        Task<CommandResult> ProcessStoreFare(string command);
        Task<CommandResult> ProcessFormOfPayment(string command);
        Task<CommandResult> ProcessTicketListDisplay(string command);
        Task<CommandResult> ProcessTicketDisplay(string command);
        Task<CommandResult> ProcessDisplayJson();
        Task<CommandResult> ProcessAddSsr(string command);
        Task<CommandResult> ProcessDeleteSsr(string command);
        Task<CommandResult> ProcessListSsr();
        Task<CommandResult> ProcessCheckIn(string command);
        Task<CommandResult> ProcessCancelCheckIn(string command);
        Task<CommandResult> ProcessAddDocument(string command);
        Task<CommandResult> ProcessDeleteDocument(string command);
        Task<CommandResult> ProcessListDocuments();
        Task<CommandResult> ProcessCheckInAll(string command);
        Task<CommandResult> ProcessRetrieveByName(string command);
        Task<CommandResult> ProcessRetrieveByFlight(string command);
        Task<CommandResult> ProcessRetrieveByPhone(string command);
        Task<CommandResult> ProcessRetrieveByTicket(string command);
        Task<CommandResult> ProcessRetrieveByFrequentFlyer(string command);
        Task<CommandResult> ProcessDisplayApis(string command);
        Task<CommandResult> ProcessAddApiAddress(string command);
        Task<CommandResult> ProcessDisplaySeatMap(string command);
        Task<CommandResult> ProcessAssignSeat(string command);
        Task<CommandResult> ProcessRemoveSeat(string command);
        Task<CommandResult> ProcessArnk(string command);
        Task<CommandResult> ProcessDeletePnr(string command);

        Task<string> GetHelpText();
        UserContext User { get; set; }
    }
}