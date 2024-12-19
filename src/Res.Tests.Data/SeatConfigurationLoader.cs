using System.Diagnostics;
using System.Text.Json;
using Res.Domain.Entities.Inventory;

namespace Res.Tests.Data
{
    public static class SeatConfigurationLoader
    {
        public static Dictionary<string, SeatConfiguration> LoadSeatConfigurations()
        {
            var configs = new Dictionary<string, SeatConfiguration>();
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
                    var config = JsonSerializer.Deserialize<SeatConfiguration>(
                        jsonContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (config != null && !string.IsNullOrEmpty(config.AircraftType))
                    {
                        configs[config.AircraftType] = config;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading configuration from {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            return configs;
        }
    }
}