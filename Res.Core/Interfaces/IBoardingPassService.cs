using Res.Domain.Entities.CheckIn;
using Res.Domain.Requests;

namespace Res.Core.Interfaces
{
    public interface IBoardingPassService
    {
        BoardingPass GenerateBoardingPass(BoardingPassRequest request);
    }
}