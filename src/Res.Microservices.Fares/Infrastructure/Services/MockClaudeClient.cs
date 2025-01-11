using Microsoft.Extensions.Logging;
using Res.Microservices.Fares.Application.Interfaces;
using System.Text.RegularExpressions;

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
            var occupancyPercentage = ExtractOccupancyPercentage(prompt);

            var response = GenerateMockResponse(timeOfDay, occupancyPercentage);
            return Task.FromResult(response);
        }

        private string ExtractTimeOfDay(string prompt)
        {
            if (prompt.Contains("MORNING")) return "MORNING";
            if (prompt.Contains("AFTERNOON")) return "AFTERNOON";
            if (prompt.Contains("EVENING")) return "EVENING";
            return "NIGHT";
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

        private string GenerateMockResponse(string timeOfDay, double occupancy)
        {
            // Base fares for different cabin classes
            var baseFares = new Dictionary<string, (decimal adult, decimal child)>
            {
                { "F", (8000m, 6000m) },
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

            // Build the response for all cabin classes
            var cabins = new Dictionary<string, Dictionary<string, object>>();
            foreach (var fare in baseFares)
            {
                var cabinClass = fare.Key;
                var timeMultiplier = timeAdjustments[timeOfDay];

                // Occupancy adjustments
                var occupancyMultiplier = occupancy switch
                {
                    > 80 => 1.2m,
                    > 60 => 1.1m,
                    > 40 => 1.0m,
                    _ => 0.9m
                };

                // Calculate fares
                var baseAdultFare = Math.Round(fare.Value.adult * timeMultiplier * occupancyMultiplier, 2);
                var baseChildFare = Math.Round(fare.Value.child * timeMultiplier * occupancyMultiplier, 2);
                var adultTax = Math.Round(baseAdultFare * 0.1m, 2);
                var childTax = Math.Round(baseChildFare * 0.1m, 2);

                var totalSeats = GetTotalSeats(cabinClass);
                var availableSeats = GetAvailableSeats(cabinClass, occupancy);
                var cabinOccupancy = ((totalSeats - availableSeats) / (double)totalSeats) * 100;

                // Create the cabin pricing data
                cabins[cabinClass] = new Dictionary<string, object>
                {
                    ["pricing"] = new[]
                    {
                        new
                        {
                            passengerType = "ADT",
                            quantity = 2,
                            baseFare = baseAdultFare,
                            taxes = adultTax,
                            total = baseAdultFare + adultTax
                        },
                        new
                        {
                            passengerType = "CHD",
                            quantity = 1,
                            baseFare = baseChildFare,
                            taxes = childTax,
                            total = baseChildFare + childTax
                        }
                    },
                    ["totalItinerary"] = (baseAdultFare + adultTax) * 2 + (baseChildFare + childTax),
                    ["cabinDetails"] = new
                    {
                        cabinClass = cabinClass,
                        totalSeats = totalSeats,
                        availableSeats = availableSeats,
                        occupancyPercentage = Math.Round(cabinOccupancy, 1),
                        demandLevel = GetDemandLevel(cabinOccupancy)
                    },
                    ["pricingFactors"] = new
                    {
                        seasonality = GetSeasonality(),
                        demandLevel = GetDemandLevel(occupancy),
                        competitionLevel = GetCompetitionLevel(),
                        daysUntilDeparture = _random.Next(30, 180),
                        cabinLoadFactor = Math.Round(occupancy, 1),
                        timeOfDay = timeOfDay
                    },
                    ["explanation"] = GenerateExplanation(timeOfDay, cabinClass, occupancy)
                };
            }

            // Serialize to JSON
            return System.Text.Json.JsonSerializer.Serialize(cabins, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        private int GetTotalSeats(string cabinClass) => cabinClass switch
        {
            "F" => 8,
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
                "F" => "First Class",
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