using Res.Domain.Entities.CheckIn;

namespace Res.Core.Interfaces
{
    public interface IApisService
    {
        Task<bool> ValidateApisData(ApisData apisData);
        Task<bool> SubmitApisData(ApisData apisData);
        Task<ApisData> BuildApisFromPnr(string recordLocator, string flightNumber);
    }
}