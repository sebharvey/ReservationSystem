using Res.Api.Models;

namespace Res.Api.Core.Services
{
    public interface IOfferService
    {
        Task<SearchResponse> Search(SearchRequest searchRequest);
        Task<SummaryResponse?> Summary(SummaryRequest summaryRequest);
    }
}