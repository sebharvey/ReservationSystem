using Res.Api.Offer.Application.DTOs;

namespace Res.Api.Offer.Application.Interfaces
{
    public interface IOfferService
    {
        Task<SearchResponse> Search(SearchRequest searchRequest);
        Task<SummaryResponse?> Summary(SummaryRequest summaryRequest);
    }
}