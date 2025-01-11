using Res.Microservices.Fares.Application.DTOs;

namespace Res.Microservices.Fares.Application.Interfaces
{
    public interface IPricingService
    {
        Task<PricingResponseDto> GetPricingRecommendation(PricingRequestDto request);
    }
}