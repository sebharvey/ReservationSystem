using Res.Api.Models;

namespace Res.Api.Core.Services;

public interface IStatusService
{
    Task<List<StatusResponse>> FlightStatus(DateTime date);
}