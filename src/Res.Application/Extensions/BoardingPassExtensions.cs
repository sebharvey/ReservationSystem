using System.Text;
using Res.Domain.Entities.CheckIn;

namespace Res.Application.Extensions
{
    public static class BoardingPassExtensions
    {
        public static string OutputBoardingPass(this BoardingPass boardingPass)
        {
            var sb = new StringBuilder();

            // Format header
            sb.AppendLine(new string('-', 50));
            sb.AppendLine("BOARDING PASS".PadLeft(25).PadRight(50));
            sb.AppendLine(new string('-', 50));

            // Passenger Information
            if (boardingPass.Passenger != null)
            {
                sb.AppendLine($"PASSENGER: {boardingPass.Passenger.LastName}/{boardingPass.Passenger.FirstName} {boardingPass.Passenger.Title}");
                sb.AppendLine($"TYPE: {boardingPass.Passenger.Type}");
            }

            // Flight info
            sb.AppendLine($"FLIGHT: {boardingPass.FlightNumber}");
            sb.AppendLine($"FROM:   {boardingPass.Origin}     TO: {boardingPass.Destination}");
            sb.AppendLine($"DATE:   {boardingPass.DepartureDate}  TIME: {boardingPass.DepartureTime}");

            // Boarding info
            sb.AppendLine($"SEAT:   {boardingPass.SeatNumber}");
            sb.AppendLine($"GROUP:  {boardingPass.BoardingGroup}");
            sb.AppendLine($"SEQ:    {boardingPass.Sequence}");

            // Gate info (if available)
            if (!string.IsNullOrEmpty(boardingPass.DepartureGate))
            {
                sb.AppendLine($"GATE:   {boardingPass.DepartureGate}");
            }

            // Baggage info
            if (boardingPass.HasCheckedBags)
            {
                sb.AppendLine($"BAGS:   {boardingPass.BaggageCount} ({boardingPass.BaggageWeight}KG)");
            }

            // Special services
            if (boardingPass.SecurityMessages.Any())
            {
                sb.AppendLine();
                sb.AppendLine("SPECIAL SERVICES:");
                foreach (var msg in boardingPass.SecurityMessages)
                {
                    sb.AppendLine($"* {msg}");
                }
            }

            // Fast track/Lounge access if available
            if (!string.IsNullOrEmpty(boardingPass.FastTrack))
            {
                sb.AppendLine($"FAST TRACK: {boardingPass.FastTrack}");
            }
            if (!string.IsNullOrEmpty(boardingPass.LoungeAccess))
            {
                sb.AppendLine($"LOUNGE: {boardingPass.LoungeAccess}");
            }

            // Barcode
            sb.AppendLine();
            sb.AppendLine($"BARCODE: {boardingPass.BarcodeData}");

            // Footer
            sb.AppendLine(new string('-', 50));
            sb.AppendLine($"CHECK-IN TIME: {boardingPass.CheckInTime:dd-MMM-yyyy HH:mm}");
            sb.AppendLine($"TICKET:        {boardingPass.TicketNumber}");

            sb.AppendLine(new string('-', 50));

            return sb.ToString();
        }

        public static string OutputBoardingPasses(this List<BoardingPass> boardingPasses)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"CHECKED IN {boardingPasses.Count} PASSENGERS");
            sb.AppendLine(new string('-', 50));

            foreach (var pass in boardingPasses)
            {
                sb.AppendLine($"PASSENGER: {pass.PassengerId}");
                sb.AppendLine($"SEAT: {pass.SeatNumber}");
                sb.AppendLine($"SEQ: {pass.Sequence}");

                if (pass.SecurityMessages.Any())
                {
                    sb.AppendLine("SPECIAL SERVICES:");
                    foreach (var msg in pass.SecurityMessages)
                    {
                        sb.AppendLine($"* {msg}");
                    }
                }
                sb.AppendLine(new string('-', 50));
            }

            return sb.ToString();
        }

    }
}