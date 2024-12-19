using Res.Domain.Entities.Pnr;

namespace Res.Infrastructure.Data
{
    public interface IPnrDataManager
    {
        Task<bool> CreatePnrAsync(Pnr pnr);
        Task<Pnr> GetPnrAsync(string recordLocator);
        Task<bool> UpdatePnrAsync(Pnr pnr);
        Task<bool> DeletePnrAsync(string recordLocator);
        Task<bool> PnrExistsAsync(string recordLocator);
        Task<List<Pnr>> GetAllPnrsAsync();
        Task<List<Pnr>> SearchByNameAsync(string lastName, string firstName = null);
        Task<List<Pnr>> SearchByFlightAsync(string flightNumber, string date);
        Task<List<Pnr>> SearchByPhoneAsync(string phoneNumber);
        Task<List<Pnr>> SearchByTicketAsync(string ticketNumber);
        Task<List<Pnr>> SearchByFrequentFlyerAsync(string ffNumber);
    }
}