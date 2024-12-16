using Res.Api.Models;

namespace Res.Api.Core.Services;

public interface ICheckinService
{
    Task<ValidateCheckInResponse?> ValidateCheckin(ValidateCheckInRequest validatCheckInRequest);
    Task<CheckInResponse?> CheckIn(CheckInRequest checkInRequest);
}