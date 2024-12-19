using Res.Domain.Entities.Pnr;
using Res.Domain.Requests;

namespace Res.Core.Interfaces
{
    public interface IFareService
    {
        Task<Pnr> AddFare(Pnr currentPnr, FareInfo fare);
        Task<Pnr> StoreFare(Pnr pnr, StoreFareRequest request);
        Task<Pnr> AddFormOfPayment(Pnr currentPnr, string fop);
        Task<Pnr> PricePnr(Pnr pnr, PricePnrRequest request);
        (decimal BaseFare, List<string> FareBases) CalculateSegmentPrices(IEnumerable<Segment> segments, string currency);
    }
}