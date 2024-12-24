using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;
using Res.Domain.Requests;
using Res.Domain.Responses;

namespace Res.Core.Interfaces
{
    public interface IReservationService
    {
        UserContext UserContext { get; set; }
        Pnr? Pnr { get; set; }
        Task<bool> CreatePnrWorkspace();
        Task<bool> AddName(string lastName, string firstName, string title, PassengerType type);
        Task<bool> AddSegment(SellSegmentRequest sellSegmentRequest);
        Task<bool> RemoveSegment(int segmentNumber);
        Task<bool> AddPhone(string phoneNumber, string type = "M");
        Task<bool> AddEmail(string emailAddress);
        Task<bool> AddAgency(AgencyRequest agencyRequest);
        Task<bool> AddRemarks(string remarkText);
        Task<bool> AddTicketArrangement(DateTime ticketTimeLimit, string validatingCarrier);
        Task<bool> CommitPnr();
        Task<bool> RetrievePnr(string recordLocator);
        Task<List<Pnr>> RetrieveAllPnrs();
        Task<List<Pnr>> RetrieveByName(string lastName, string firstName = null);
        Task<List<Pnr>> RetrieveByFlight(string flightNumber, string date);
        Task<List<Pnr>> RetrieveByPhone(string phoneNumber);
        Task<List<Pnr>> RetrieveByTicket(string ticketNumber);
        Task<List<Pnr>> RetrieveByFrequentFlyer(string ffNumber);
        Task<Segment> SellSegment(FlightInventory flightToSell, string bookingClass, int quantity);
        Task<bool> DeletePnr(string recordLocator);
        Task<Pnr?> LoadCurrentPnr();
        Task<bool> AddArnkSegment(int? position);
        Task IgnoreSession();
    }
}