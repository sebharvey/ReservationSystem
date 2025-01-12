using System.Text;

namespace Res.Microservices.Inventory.Application.Importer
{
    public class FlightDataHelper
    {
        public class FlightDataRecord
        {
            public string FlightNo { get; set; }
            public string Origin { get; set; }
            public string Dest { get; set; }
            public TimeOnly Dep { get; set; }
            public TimeOnly Arr { get; set; }
            public string Freq { get; set; }
            public string AircraftType { get; set; }
        }

        public static List<FlightDataRecord> LoadFlightData(string filePath)
        {
            var records = new List<FlightDataRecord>();
            var lines = File.ReadAllLines(filePath);

            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var record = ParseCsvLine(lines[i]);
                if (record != null)
                {
                    records.Add(record);
                }
            }

            // Group by flight number and take the one with highest frequency count
            return records.GroupBy(r => r.FlightNo)
                .Select(g => g.OrderByDescending(r => CountFrequency(r.Freq)).First())
                .ToList();
        }

        private static FlightDataRecord ParseCsvLine(string line)
        {
            try
            {
                var fields = ParseCsvFields(line);
                if (fields.Length != 7) return null;

                return new FlightDataRecord
                {
                    FlightNo = fields[0],
                    Origin = fields[1],
                    Dest = fields[2],
                    Dep = TimeOnly.ParseExact(fields[3], "HH:mm:ss"),
                    Arr = TimeOnly.ParseExact(fields[4], "HH:mm:ss"),
                    Freq = fields[5],
                    AircraftType = fields[6]
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string[] ParseCsvFields(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(line[i]);
                }
            }

            fields.Add(currentField.ToString().Trim());
            return fields.ToArray();
        }

        private static int CountFrequency(string freq)
        {
            return freq.Count(c => c != '_');
        }

        public static List<DateTime> GetOperatingDates(string frequency, DateTime startDate, int daysToGenerate)
        {
            var dates = new List<DateTime>();
            var currentDate = startDate;

            while (dates.Count < daysToGenerate)
            {
                // Get day of week (0 = Sunday, so adjust to match frequency string where 1 = Monday)
                int dayIndex = ((int)currentDate.DayOfWeek + 6) % 7;

                // Check if flight operates on this day
                if (frequency[dayIndex] != '_')
                {
                    dates.Add(currentDate);
                }

                currentDate = currentDate.AddDays(1);
            }

            return dates;
        }

        public static bool ValidateFrequencyString(string frequency)
        {
            if (string.IsNullOrEmpty(frequency) || frequency.Length != 7)
                return false;

            return frequency.All(c => c == '_' || (c >= '1' && c <= '7'));
        }
    }
}