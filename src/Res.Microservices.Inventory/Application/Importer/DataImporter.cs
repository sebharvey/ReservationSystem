using System.Diagnostics;
using Res.Microservices.Inventory.Domain.Entities;

namespace Res.Microservices.Inventory.Application.Importer
{
    public class DataImporter
    {
        public List<Flight> LoadFlights(List<AircraftConfig> aircraftConfigs)
        {
            // Import flight data
            List<Flight> flights = new List<Flight>();

            var flightDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "FlightData.csv");

            if (!File.Exists(flightDataPath))
            {
                throw new FileNotFoundException($"Flight data file not found at: {flightDataPath}");
            }

            var flightData = FlightDataHelper.LoadFlightData(flightDataPath);

            // Calculate date range
            var startDate = DateTime.Today;
            const int daysToGenerate = 331;

            foreach (var flight in flightData)
            {
                if (!FlightDataHelper.ValidateFrequencyString(flight.Freq))
                {
                    Debug.WriteLine($"Warning: Invalid frequency string '{flight.Freq}' for flight {flight.FlightNo}");
                    continue;
                }

                // Get operating dates based on frequency
                var operatingDates = FlightDataHelper.GetOperatingDates(flight.Freq, startDate, daysToGenerate);

                foreach (var operatingDate in operatingDates)
                {
                    // Calculate arrival date/time
                    var departureDateTime = operatingDate.Date.Add(flight.Dep.ToTimeSpan());
                    var arrivalDateTime = operatingDate.Date.Add(flight.Arr.ToTimeSpan());

                    // If arrival time is before departure time, it must be next day
                    if (arrivalDateTime < departureDateTime)
                    {
                        arrivalDateTime = arrivalDateTime.AddDays(1);
                    }

                    // Get seat capacities from configuration
                    AircraftConfig seatConfig = aircraftConfigs.Single(item => item.AircraftType == flight.AircraftType);
                    var seats = new Dictionary<string, int>();

                    foreach (var cabin in seatConfig.Cabins)
                    {
                        var seatCount = CalculateCabinCapacity(cabin.Value);
                        seats[cabin.Key] = seatCount;
                    }

                    flights.Add(new Flight
                    {
                        FlightNo = flight.FlightNo,
                        From = flight.Origin,
                        To = flight.Dest,
                        Departure = departureDateTime,
                        Arrival = arrivalDateTime,
                        AircraftType = flight.AircraftType,
                        Seats = seats
                    });
                }
            }

            return flights;
        }


        private int CalculateCabinCapacity(AircraftConfigCabin cabin)
        {
            // Calculate total number of seats in the cabin
            var rowCount = cabin.LastRow - cabin.FirstRow + 1;
            var seatsPerRow = cabin.SeatLetters.Count;
            var totalSeats = rowCount * seatsPerRow;

            // Subtract blocked seats if any
            if (cabin.BlockedSeats != null)
            {
                totalSeats -= cabin.BlockedSeats.Count;
            }

            return totalSeats;
        }

        public List<AircraftConfig> LoadAircraftConfigs()
        {
            // Load seat configurations
            return SeatConfigurationLoader.LoadSeatConfigurations();
        }
    }
}
