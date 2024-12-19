using System.Text;
using Res.Domain.Entities.SeatMap;

namespace Res.Application.Extensions
{
    public static class SeatMapExtensions
    {
        public static string OutputSeatMap(this SeatMap seatMap)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"SEAT MAP FOR {seatMap.FlightNumber} {seatMap.DepartureDate}");
            sb.AppendLine($"AIRCRAFT: {seatMap.AircraftType}");
            sb.AppendLine();

            foreach (var cabin in seatMap.Cabins)
            {
                sb.AppendLine($"{cabin.CabinName} ({cabin.CabinCode})");
                sb.AppendLine(new string('-', 50));

                // Header row
                sb.Append("    "); // Space for row numbers
                var seatLetters = cabin.Rows.First().Seats.Select(s => s.SeatNumber.Last()).ToList();
                foreach (var letter in seatLetters)
                {
                    sb.Append($" {letter} ");
                }
                sb.AppendLine();

                foreach (var row in cabin.Rows)
                {
                    // Row number
                    sb.Append($"{row.RowNumber,3} ");

                    foreach (var seat in row.Seats)
                    {
                        string symbol = seat.Status switch
                        {
                            "A" => seat.IsExit ? "E" : seat.IsBulkhead ? "B" : ".",
                            "X" => "X",
                            "B" => "*",
                            _ => "?"
                        };

                        sb.Append($" {symbol} ");
                    }

                    // Add legend for special rows and occupied seats
                    if (row.Seats.Any(s => s.IsExit))
                        sb.Append(" <-- Exit Row");
                    else if (row.Seats.Any(s => s.IsBulkhead))
                        sb.Append(" <-- Bulkhead");
                    else
                    {
                        var blockedSeat = row.Seats.FirstOrDefault(s => !string.IsNullOrEmpty(s.BlockedReason));
                        if (blockedSeat != null)
                            sb.Append($" <-- {blockedSeat.BlockedReason}");
                    }

                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            // Legend
            sb.AppendLine("LEGEND:");
            sb.AppendLine(". Available");
            sb.AppendLine("X Occupied");
            sb.AppendLine("* Blocked");
            sb.AppendLine("E Exit Row");
            sb.AppendLine("B Bulkhead");

            return sb.ToString();
        }
    }
}