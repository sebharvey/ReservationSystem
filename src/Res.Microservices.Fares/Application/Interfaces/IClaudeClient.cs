namespace Res.Microservices.Fares.Application.Interfaces
{
    public interface IClaudeClient
    {
        Task<string> GetPricingResponse(string prompt);
    }
}