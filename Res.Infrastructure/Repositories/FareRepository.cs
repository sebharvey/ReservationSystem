using Res.Domain.Entities.Fare;
using Res.Infrastructure.Interfaces;

namespace Res.Infrastructure.Repositories
{
    public class FareRepository : IFareRepository
    {
        private static readonly List<FareFamily> _fareFamilies = new()
        {
            new FareFamily
            {
                Code = "BASIC",
                Name = "Basic Economy",
                Description = "Best value fare with basic services",
                Benefits = new List<string>
                {
                    "Standard seat assignment at check-in",
                    "1 carry-on bag"
                },
                IsRefundable = false,
                IsChangeable = false,
                ChangeFee = 150.00m,
                HasSeatSelection = false,
                HasBaggageIncluded = false,
                HasPriorityBoarding = false,
                HasLoungeAccess = false,
                PriceMultiplier = 1.0m
            },
            new FareFamily
            {
                Code = "CLASSIC",
                Name = "Classic",
                Description = "Standard fare with flexibility",
                Benefits = new List<string>
                {
                    "Free seat selection",
                    "1 checked bag",
                    "Changes permitted"
                },
                IsRefundable = false,
                IsChangeable = true,
                ChangeFee = 50.00m,
                HasSeatSelection = true,
                HasBaggageIncluded = true,
                HasPriorityBoarding = false,
                HasLoungeAccess = false,
                PriceMultiplier = 1.25m
            },
            new FareFamily
            {
                Code = "FLEX",
                Name = "Flex",
                Description = "Fully flexible fare with premium benefits",
                Benefits = new List<string>
                {
                    "Free seat selection",
                    "2 checked bags",
                    "Priority check-in",
                    "Lounge access",
                    "Fully refundable",
                    "Free changes"
                },
                IsRefundable = true,
                IsChangeable = true,
                ChangeFee = 0.00m,
                HasSeatSelection = true,
                HasBaggageIncluded = true,
                HasPriorityBoarding = true,
                HasLoungeAccess = true,
                PriceMultiplier = 1.75m
            }
        };

        public List<FareFamily> FareFamilies
        {
            get => _fareFamilies;
            //set => _fareFamilies = value;
        }
    }
}
