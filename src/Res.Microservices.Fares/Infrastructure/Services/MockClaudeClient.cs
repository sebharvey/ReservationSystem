using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Res.Microservices.Fares.Application.Interfaces;

namespace Res.Microservices.Fares.Infrastructure.Services
{
    public class MockClaudeClient : IClaudeClient
    {
        private readonly ILogger<MockClaudeClient> _logger;
        private readonly Random _random;

        public MockClaudeClient(ILogger<MockClaudeClient> logger)
        {
            _logger = logger;
            _random = new Random();
        }

        public Task<string> GetPricingResponse(string prompt)
        {
            _logger.LogInformation("Generating mock pricing response");

            // Extract key information from the prompt
            var timeOfDay = ExtractTimeOfDay(prompt);
            var cabinClass = ExtractCabinClass(prompt);
            var occupancyPercentage = ExtractOccupancyPercentage(prompt);

            var response = GenerateMockResponse(timeOfDay, cabinClass, occupancyPercentage);
            return Task.FromResult(response);
        }

        private string ExtractTimeOfDay(string prompt)
        {
            if (prompt.Contains("MORNING")) return "MORNING";
            if (prompt.Contains("AFTERNOON")) return "AFTERNOON";
            if (prompt.Contains("EVENING")) return "EVENING";
            return "NIGHT";
        }

        private string ExtractCabinClass(string prompt)
        {
            if (prompt.Contains("Cabin Class: J")) return "J";
            if (prompt.Contains("Cabin Class: W")) return "W";
            return "Y";
        }

        private double ExtractOccupancyPercentage(string prompt)
        {
            var match = Regex.Match(prompt, @"Current Occupancy: (\d+\.?\d*)%");
            if (match.Success && double.TryParse(match.Groups[1].Value, out double occupancy))
            {
                return occupancy;
            }
            return 50.0; // Default occupancy
        }

        private string GenerateMockResponse(string timeOfDay, string cabinClass, double occupancy)
        {
            // Base fares for different cabin classes
            var baseFares = new Dictionary<string, (decimal adult, decimal child)>
            {
                { "J", (4500m, 3375m) },
                { "W", (2200m, 1650m) },
                { "Y", (1200m, 900m) }
            };

            // Time of day adjustments
            var timeAdjustments = new Dictionary<string, decimal>
            {
                { "MORNING", 1.0m },
                { "AFTERNOON", 0.9m },
                { "EVENING", 0.95m },
                { "NIGHT", 0.85m }
            };

            // Occupancy adjustments
            var occupancyMultiplier = occupancy switch
            {
                > 80 => 1.2m,
                > 60 => 1.1m,
                > 40 => 1.0m,
                _ => 0.9m
            };

            // Calculate adjusted fares
            var (baseAdultFare, baseChildFare) = baseFares[cabinClass];
            var timeMultiplier = timeAdjustments[timeOfDay];

            var adjustedAdultFare = Math.Round(baseAdultFare * timeMultiplier * occupancyMultiplier, 2);
            var adjustedChildFare = Math.Round(baseChildFare * timeMultiplier * occupancyMultiplier, 2);

            var adultTax = Math.Round(adjustedAdultFare * 0.1m, 2);
            var childTax = Math.Round(adjustedChildFare * 0.1m, 2);

            return $$"""
            {
                "pricing": [
                    {
                        "passengerType": "ADT",
                        "quantity": 2,
                        "baseFare": {{adjustedAdultFare}},
                        "taxes": {{adultTax}},
                        "total": {{adjustedAdultFare + adultTax}}
                    },
                    {
                        "passengerType": "CHD",
                        "quantity": 1,
                        "baseFare": {{adjustedChildFare}},
                        "taxes": {{childTax}},
                        "total": {{adjustedChildFare + childTax}}
                    }
                ],
                "totalItinerary": {{(adjustedAdultFare + adultTax) * 2 + (adjustedChildFare + childTax)}},
                "cabinDetails": {
                    "cabinClass": "{{cabinClass}}",
                    "totalSeats": {{GetTotalSeats(cabinClass)}},
                    "availableSeats": {{GetAvailableSeats(cabinClass, occupancy)}},
                    "occupancyPercentage": {{occupancy}},
                    "demandLevel": "{{GetDemandLevel(occupancy)}}"
                },
                "pricingFactors": {
                    "seasonality": "{{GetSeasonality()}}",
                    "demandLevel": "{{GetDemandLevel(occupancy)}}",
                    "competitionLevel": "{{GetCompetitionLevel()}}",
                    "daysUntilDeparture": {{_random.Next(30, 180)}},
                    "cabinLoadFactor": {{occupancy}},
                    "timeOfDay": "{{timeOfDay}}"
                },
                "explanation": "{{GenerateExplanation(timeOfDay, cabinClass, occupancy)}}"
            }
            """;
        }

        private int GetTotalSeats(string cabinClass) => cabinClass switch
        {
            "J" => 48,
            "W" => 56,
            "Y" => 200,
            _ => 180
        };

        private int GetAvailableSeats(string cabinClass, double occupancy)
        {
            var total = GetTotalSeats(cabinClass);
            return (int)(total * (1 - occupancy / 100));
        }

        private string GetDemandLevel(double occupancy)
        {
            if (occupancy > 80) return "HIGH";
            if (occupancy > 60) return "MODERATE";
            return "LOW";
        }

        private string GetSeasonality()
        {
            var month = DateTime.Now.Month;
            if (new[] { 6, 7, 8, 12 }.Contains(month)) return "PEAK";
            if (new[] { 4, 5, 9, 10 }.Contains(month)) return "SHOULDER";
            return "OFF_PEAK";
        }

        private string GetCompetitionLevel()
        {
            return _random.Next(3) switch
            {
                0 => "HIGH",
                1 => "MODERATE",
                _ => "LOW"
            };
        }

        private string GenerateExplanation(string timeOfDay, string cabinClass, double occupancy)
        {
            var timePhrase = timeOfDay.ToLower();
            var cabinPhrase = cabinClass switch
            {
                "J" => "Business Class",
                "W" => "Premium Economy",
                _ => "Economy"
            };
            var occupancyPhrase = occupancy switch
            {
                > 80 => "very high",
                > 60 => "high",
                > 40 => "moderate",
                _ => "low"
            };

            var seasonality = GetSeasonality();
            var competition = GetCompetitionLevel().ToLower();

            return $"Pricing reflects {timePhrase} departure in {cabinPhrase} with {occupancyPhrase} occupancy ({occupancy:F1}%). " +
                   $"Currently in {seasonality.ToLower()} season with {competition} competition levels. " +
                   $"{(occupancy > 60 ? "Premium pricing applied due to high demand. " : "")}" +
                   $"{(timeOfDay == "NIGHT" || timeOfDay == "AFTERNOON" ? "Time-based discount applied. " : "")}";
        }
    }
}