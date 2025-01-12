using Res.Api.Offer.Domain.Models;

namespace Res.Api.Offer.Domain.Interfaces
{
    public interface IFlightSearchService
    {
        Task<SearchResponse> SearchFlightsAsync(SearchRequest request);
    }
}