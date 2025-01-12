using Res.Api.Offer.Domain.Models;

namespace Res.Api.Offer.Domain.Interfaces
{
    public interface IInventoryService
    {
        Task<List<FlightAvailability>> GetAvailabilityAsync(string from, string to, DateTime date);
    }
}