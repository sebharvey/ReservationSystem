using Res.Domain.Entities.Fare;

namespace Res.Infrastructure.Interfaces
{
    public interface IFareRepository
    {
        List<FareFamily> FareFamilies
        {
            get;
            //set => _fareFamilies = value;
        }
    }
}