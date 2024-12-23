using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Pnr;
using Res.Domain.Requests;

namespace Res.Core.Interfaces
{
    public interface IBoardingPassService
    {
        Pnr Pnr { get; set; }
        BoardingPass GenerateBoardingPass(BoardingPassRequest request);
    }
}