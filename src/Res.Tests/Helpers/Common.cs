using Res.Domain.Entities.Inventory;
using System.Globalization;

namespace Res.Tests.Helpers
{
    internal class Common
    {
        public static string ToAirlineShortDate(DateTime date)
        {
            return date.ToString("ddMMM", CultureInfo.InvariantCulture).ToUpper();
        }

        internal static string FindFlightAndGenerateLongSellCommand(List<FlightInventory> inventory, string flightNo, int qty, string cabin)
        {
            var flight = inventory
                .Where(flight =>
                {
                    var departureDate = DateTime.ParseExact(flight.DepartureDate, "ddMMM", CultureInfo.InvariantCulture).Date;
                    var departureTime = TimeOnly.ParseExact(flight.DepartureTime, "HHmm", CultureInfo.InvariantCulture);
                    var departureDateTime = departureDate.Add(departureTime.ToTimeSpan());

                    return flight.FlightNo == flightNo && departureDateTime > DateTime.Now;
                })
                .OrderBy(flight => DateTime.ParseExact(flight.DepartureDate, "ddMMM", CultureInfo.InvariantCulture).Date)
                .First();

            return $"SS {flightNo}{cabin}{flight.DepartureDate}{flight.From}{flight.To}{qty}";
        }

        internal static string FindFlightAndGenerateLongSellCommand(List<FlightInventory> inventory, int qty, string cabin, string from, string to, string departureDate, bool nextAvailable = false)
        {
            FlightInventory flight;

            if (nextAvailable)
            {
                flight = inventory
                    .First(f => DateTime.ParseExact(f.DepartureDate, "ddMMM", CultureInfo.InvariantCulture).Date > DateTime.Today ||
                                (DateTime.ParseExact(f.DepartureDate, "ddMMM", CultureInfo.InvariantCulture).Date == DateTime.Today &&
                                 TimeOnly.ParseExact(f.DepartureTime, "HHmm", CultureInfo.InvariantCulture) > TimeOnly.FromDateTime(DateTime.Now)));
            }
            else
            {
                flight = inventory.First(item => item.From == from && item.To == to && item.DepartureDate == departureDate);
            }

            return $"SS {flight.FlightNo}{cabin}{flight.DepartureDate}{flight.From}{flight.To}{qty}";
        }
    }
}
