using Res.Domain.Dto;

namespace Res.Application.ReservationSystem
{
    public interface IReservationSystem
    {
        Task<CommandResult> ProcessCommand(string crypticCommand, string token);
    }
}