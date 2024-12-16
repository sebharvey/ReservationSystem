using Res.Domain.Entities.Pnr;

namespace Res.Core.Interfaces
{
    public interface ISpecialServiceRequestsService
    {
        Pnr Pnr { get; set; }
        Task<bool> AddSsr(string code, int passengerId, int segmentNumber, string? text);
        Task<bool> DeleteSsr(int ssrId);
    }
}