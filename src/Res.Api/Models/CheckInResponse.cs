using Res.Domain.Entities.CheckIn;

namespace Res.Api.Models;

public class CheckInResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<BoardingPass>? BoardingPasses { get; set; }
}