using System.Text;
using Res.Core.Common.Helpers;
using Res.Domain.Entities.Inventory;

namespace Res.Application.Extensions
{
    public static class InventoryExtensions
    {
        public static string OutputSearchResults(this List<FlightInventory> flights)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < flights.Count; i++)
            {
                var flight = flights[i];

                var line = $"{i + 1} {flight.FlightNo} " +
                           $"{flight.DepartureDate} " +
                           string.Join(" ", flight.Seats.Select(kvp => $"{kvp.Key}{kvp.Value}")) + " " +
                           $"{flight.From}{flight.To} " +
                           $"{flight.DepartureTime} ";

                var arrivalDateTime = DateTimeHelper.CombineDateAndTime(flight.ArrivalDate, flight.ArrivalTime);
                var departureDateTime = DateTimeHelper.CombineDateAndTime(flight.DepartureDate, flight.DepartureTime);

                int offset = (arrivalDateTime.Date - departureDateTime.Date).Days;

                if (offset != 0)
                {
                    line += $"{flight.ArrivalTime}+{offset}";
                }
                else
                {
                    line += $"{flight.ArrivalTime}  ";
                }

                line += $" *{flight.AircraftType}";

                sb.AppendLine(line);
            }

            return sb.ToString();
        }

    }
}