using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;
using Res.Domain.Requests;
using Res.Domain.Responses;

namespace Res.Core.Interfaces
{
    public interface IReservationService
    {
        Task<Pnr> CreatePnrWorkspace(UserContext user);
        Task<Pnr> AddName(Pnr pnr, string lastName, string firstName, string title, PassengerType type);
        Task<Pnr> AddSegment(Pnr pnr, SellSegmentRequest sellSegmentRequest);
        Task<Pnr> RemoveSegment(Pnr pnr, int segmentNumber);
        Task<Pnr> AddPhone(Pnr pnr, string phoneNumber, string type = "M");
        Task<Pnr> AddEmail(Pnr pnr, string emailAddress);
        Task<Pnr> AddAgency(Pnr pnr, AgencyRequest agencyRequest);
        Task<Pnr> AddRemarks(Pnr pnr, string remarkText);
        Task<Pnr> AddTicketArrangement(Pnr pnr, DateTime ticketTimeLimit, string validatingCarrier);
        Task<Pnr> CommitPnr(Pnr pnr);
        Task<Pnr?> RetrievePnr(string recordLocator);
        Task<List<Pnr>> RetrieveAllPnrs();
        Task<List<Pnr>> RetrieveByName(string lastName, string firstName = null);
        Task<List<Pnr>> RetrieveByFlight(string flightNumber, string date);
        Task<List<Pnr>> RetrieveByPhone(string phoneNumber);
        Task<List<Pnr>> RetrieveByTicket(string ticketNumber);
        Task<List<Pnr>> RetrieveByFrequentFlyer(string ffNumber);
        Task<(Segment segment, Pnr pnr)> SellSegment(Pnr pnr, FlightInventory flightToSell, string bookingClass, int quantity);
        Task<bool> DeletePnr(string recordLocator);

    }
}