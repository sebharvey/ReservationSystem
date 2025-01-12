using System.Diagnostics;
using System.Text.Json;
using Res.Microservices.Inventory.Domain.Entities;

namespace Res.Microservices.Inventory.Application.Importer
{
    public static class SeatConfigurationLoader
    {
        public static List<AircraftConfig> LoadSeatConfigurations()
        {
            List<AircraftConfig> aircraftConfigs = new List<AircraftConfig>();

            var configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Res.Tests.Data", "Data", "AircraftConfig");

            if (!Directory.Exists(configDir))
            {
                throw new DirectoryNotFoundException($"Aircraft configuration directory not found at: {configDir}");
            }

            foreach (var file in Directory.GetFiles(configDir, "*.json"))
            {
                try
                {
                    var jsonContent = File.ReadAllText(file);
                    var config = JsonSerializer.Deserialize<AircraftConfig>(
                        jsonContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    
                    aircraftConfigs.Add(new AircraftConfig {AircraftType = config.AircraftType, Cabins = config.Cabins });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading configuration from {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            return aircraftConfigs;
        }
    }
}