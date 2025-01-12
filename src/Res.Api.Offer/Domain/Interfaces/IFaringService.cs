using Res.Api.Offer.Domain.Models;

namespace Res.Api.Offer.Domain.Interfaces
{
    public interface IFaringService
    {
        Task<List<FlightPrice>> GetPricesAsync(List<string> flightNumbers, string currency);
    }
}