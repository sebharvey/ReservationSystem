using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Res.Microservices.Fares.Application.DTOs;
using Res.Microservices.Fares.Application.Interfaces;

namespace Res.Microservices.Fares.Application.Services
{
    public class PricingService : IPricingService
    {
        private readonly IClaudeClient _claudeClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PricingService> _logger;

        public PricingService(IClaudeClient claudeClient, IMemoryCache cache, ILogger<PricingService> logger)
        {
            _claudeClient = claudeClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PricingResponseDto> GetPricingRecommendation(PricingRequestDto request)
        {
            var flightPricingTasks = request.Flights
                .Select(flight => GetFlightPricing(request, flight))
                .ToList();

            var flightPricings = await Task.WhenAll(flightPricingTasks);

            return new PricingResponseDto
            {
                Flights = flightPricings.ToList()
            };
        }

        private async Task<FlightPricingDto> GetFlightPricing(PricingRequestDto request, FlightRequestDto flight)
        {
            var cabinPricingTasks = flight.CabinInventory
                .Select(async cabin =>
                {
                    var cacheKey = GenerateCacheKey(request, flight, cabin.Key);

                    if (!_cache.TryGetValue(cacheKey, out CabinPricingDto cabinPricing))
                    {
                        var prompt = BuildPricingPrompt(request, flight, cabin.Key);
                        var claudeResponse = await _claudeClient.GetPricingResponse(prompt);
                        cabinPricing = JsonSerializer.Deserialize<CabinPricingDto>(claudeResponse);

                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                        _cache.Set(cacheKey, cabinPricing, cacheOptions);
                    }

                    return new KeyValuePair<string, CabinPricingDto>(cabin.Key, cabinPricing);
                });

            var cabinPricings = await Task.WhenAll(cabinPricingTasks);

            return new FlightPricingDto
            {
                FlightNumber = flight.FlightNumber,
                DepartureTime = flight.DepartureTime,
                Cabins = cabinPricings.ToDictionary(x => x.Key, x => x.Value)
            };
        }

        private string BuildPricingPrompt(
            PricingRequestDto request,
            FlightRequestDto flight,
            string cabinClass)
        {
            var inventory = flight.CabinInventory[cabinClass];
            var timeOfDay = GetTimeOfDay(flight.DepartureTime);
            var timeBasedDiscount = CalculateTimeBasedDiscount(flight.DepartureTime);
            var occupancyPercentage = ((inventory.TotalSeats - inventory.AvailableSeats) / (double)inventory.TotalSeats) * 100;

            return $@"You are an airline pricing expert. Please provide competitive fare recommendations for the following flight:

Route: {request.Origin}-{request.Destination}
Date: {request.TravelDate.ToShortDateString()}
Flight: {flight.FlightNumber}
Departure Time: {flight.DepartureTime:hh\\:mm}
Time of Day: {timeOfDay} (Apply {timeBasedDiscount}% discount to base fares)
Cabin Class: {cabinClass}
Passenger Mix: {string.Join(",", request.Passengers.Select(p => $"{p.PtcCode}{p.Quantity}"))}

Cabin Inventory:
- Total Seats: {inventory.TotalSeats}
- Available Seats: {inventory.AvailableSeats}
- Current Occupancy: {occupancyPercentage:F1}%

Consider the following factors in your pricing:
1. Seasonality (peak/off-peak)
2. Day of week
3. Time until departure ({(request.TravelDate - DateTime.Now).Days} days)
4. Route popularity
5. Competition levels
6. Passenger mix (adult/child/infant)
7. Current cabin occupancy (higher prices as occupancy increases)
8. Time of day pricing (apply specified discount)
9. Available inventory

Cabin Class Guidelines:
{GetCabinClassGuidelines(cabinClass)}

Time-Based Pricing Rules:
- Morning (05:00-11:59): Standard pricing
- Afternoon (12:00-16:59): 10% discount
- Evening (17:00-20:59): 5% discount
- Night (21:00-04:59): 15-20% discount

Occupancy-Based Pricing Rules:
- Below 40%: Apply 10% discount
- 40-60%: Standard pricing
- 61-80%: Add 10% premium
- Above 80%: Add 20% premium

Child Fare Rules:
- Ages 2-11: 75% of adult fare
- Under 2 (infant): 10% of adult fare when on lap

Please provide pricing recommendations in JSON format following this exact structure (use realistic market prices for the route and cabin):
{{
    ""pricing"": [
        {{
            ""passengerType"": ""string"",
            ""quantity"": number,
            ""baseFare"": number,
            ""taxes"": number,
            ""total"": number
        }}
    ],
    ""totalItinerary"": number,
    ""cabinDetails"": {{
        ""cabinClass"": ""{cabinClass}"",
        ""totalSeats"": {inventory.TotalSeats},
        ""availableSeats"": {inventory.AvailableSeats},
        ""occupancyPercentage"": {occupancyPercentage:F1},
        ""demandLevel"": ""string""
    }},
    ""pricingFactors"": {{
        ""seasonality"": ""string"",
        ""demandLevel"": ""string"",
        ""competitionLevel"": ""string"",
        ""daysUntilDeparture"": {(request.TravelDate - DateTime.Now).Days},
        ""cabinLoadFactor"": {occupancyPercentage:F1},
        ""timeOfDay"": ""{timeOfDay}""
    }},
    ""explanation"": ""string""
}}

Ensure that:
1. Pricing reflects the current occupancy level ({occupancyPercentage:F1}%)
2. Time of day discount of {timeBasedDiscount}% is applied
3. Taxes are calculated at 10% of base fare
4. Child fares follow the specified rules
5. Explanation includes key factors affecting the price";
        }

        private string GetCabinClassGuidelines(string cabinClass)
        {
            return cabinClass switch
            {
                "F" => @"First Class Pricing:
- Premium pricing with highest comfort level
- Typical range: $5000-15000 for long-haul
- Usually 2-3x Business Class fares
- Heavy focus on service and exclusivity",

                "J" => @"Business Class Pricing:
- Premium pricing for business travelers
- Typical range: $2500-8000 for long-haul
- Usually 2-4x Economy fares
- Focus on comfort and service",

                "W" => @"Premium Economy Pricing:
- Mid-tier pricing between Economy and Business
- Typical range: $1000-3000 for long-haul
- Usually 1.5-2.5x Economy fares
- Enhanced comfort over Economy",

                "Y" => @"Economy Class Pricing:
- Base level pricing
- Typical range: $500-2000 for long-haul
- Competitive pricing essential
- High price sensitivity",

                _ => "Standard cabin pricing applies"
            };
        }

        private int CalculateTimeBasedDiscount(TimeSpan time)
        {
            return time.Hours switch
            {
                // Early morning and late night flights get bigger discounts
                >= 0 and < 6 => 20,
                >= 21 => 15,
                >= 13 and < 16 => 10,
                _ => 0
            };
        }

        private string GetTimeOfDay(TimeSpan time)
        {
            return time.Hours switch
            {
                >= 5 and < 12 => "MORNING",
                >= 12 and < 17 => "AFTERNOON",
                >= 17 and < 21 => "EVENING",
                _ => "NIGHT"
            };
        }

        private string GenerateCacheKey(PricingRequestDto request, FlightRequestDto flight, string cabinClass)
        {
            return $"{request.Origin}-{request.Destination}-{request.TravelDate:yyyyMMdd}-{flight.FlightNumber}-{cabinClass}";
        }
    }
}