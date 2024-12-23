using System.Text;
using Res.Core.Common.Helpers;
using Res.Domain.Entities.Pnr;
using Res.Domain.Entities.Ticket;

namespace Res.Application.Extensions
{
    public static class TicketExtensions
    {
        public static string OutputTickets(this List<Ticket> tickets, Pnr pnr)
        {
            var sb = new StringBuilder();
            sb.AppendLine("TICKETS ISSUED:");

            foreach (var ticket in tickets)
            {
                var passenger = pnr.Data.Passengers.First(p => p.PassengerId == ticket.PassengerId);
                sb.AppendLine($"PAX {passenger.LastName}/{passenger.FirstName}");
                sb.AppendLine($"TKT: {ticket.TicketNumber}");

                foreach (var coupon in ticket.Coupons)
                {
                    sb.AppendLine($"  CPN {coupon.CouponNumber}: {coupon.FlightNumber} {coupon.Origin}{coupon.Destination} " +
                                  $"{coupon.BookingClass} {coupon.DepartureDate} {coupon.Status}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string OutputTicket(this Ticket ticket, Pnr pnr)
        {
            var sb = new StringBuilder();
            var passenger = pnr.Data.Passengers.First(p => p.PassengerId == ticket.PassengerId);
            var fare = pnr.Data.Fares.First(f => f.PassengerId == ticket.PassengerId);

            // Header section
            sb.AppendLine(new string('-', 75));
            sb.AppendLine($"ELECTRONIC TICKET DISPLAY        {DateTime.Now:dd-MMM-yy HH:mm}");
            sb.AppendLine(new string('-', 75));

            var ticketFop = pnr.Data.FormOfPayment.Split("/");

            // Ticket information
            sb.AppendLine($"TICKET:     {ticket.TicketNumber}");
            sb.AppendLine($"ISSUED:     {ticket.IssueDate:ddMMMyy}");
            sb.AppendLine($"PASSENGER:  {passenger.LastName}/{passenger.FirstName} {passenger.Title}");
            sb.AppendLine($"PNR:        {ticket.PnrLocator}");
            sb.AppendLine($"ISSUED BY:  {ticket.IssuingOffice}");
            sb.AppendLine($"IATA:       {pnr.Data.Agency.IataNumber}");
            sb.AppendLine($"FOP:        {PaymentHelper.MaskCreditCard(ticketFop[1], ticketFop[2], ticketFop[3])} {ticketFop[4]}");
            sb.AppendLine($"FARE:       {fare.Currency} {ticket.FareAmount:F2}");
            sb.AppendLine($"STATUS:     {ticket.Status}");

            // Coupon section
            sb.AppendLine();
            sb.AppendLine("COUPON DETAILS");
            sb.AppendLine(new string('-', 75));
            sb.AppendLine("CPN FLT    CL DATE    ROUTE          ST    FARE BASIS     VALUE");
            sb.AppendLine(new string('-', 75));

            foreach (var coupon in ticket.Coupons)
            {
                string route = $"{coupon.Origin}-{coupon.Destination}".PadRight(13);
                string value = $"{coupon.FareAmount:F2}".PadLeft(8);

                sb.AppendLine($"{coupon.CouponNumber,-3} {coupon.FlightNumber,-6} {coupon.BookingClass,-2} " +
                              $"{coupon.DepartureDate,-7} {route} {coupon.Status,-5} {coupon.FareBasis,-12} " +
                              $"{fare.Currency} {value}");
            }

            // Endorsements and fare calculation
            sb.AppendLine();
            sb.AppendLine("ENDORSEMENTS");
            sb.AppendLine(new string('-', 75));
            sb.AppendLine(fare.FareRestrictions);

            sb.AppendLine();
            sb.AppendLine("FARE CALCULATION");
            sb.AppendLine(new string('-', 75));
            sb.AppendLine(GenerateFareCalculation(ticket, pnr));

            if (!string.IsNullOrEmpty(pnr.Data.FormOfPayment))
            {
                var fop = pnr.Data.FormOfPayment.Split("/");
                string paymentInfo = fop[0] switch
                {
                    "CC" => PaymentHelper.MaskCreditCard(fop[1], fop[2], fop[3]),
                    "CA" => "CASH",
                    "MS" => $"MISC PAYMENT - {fop[1]}",
                    _ => "UNKNOWN PAYMENT TYPE"
                };
                sb.AppendLine($"FOP:        {paymentInfo}");
            }

            return sb.ToString();
        }

        private static string GenerateFareCalculation(Ticket ticket, Pnr pnr)
        {
            var segments = pnr.Data.Segments;
            var fare = pnr.Data.Fares.First(f => f.PassengerId == ticket.PassengerId);

            var route = string.Join(" ", segments.Select(s => $"{s.Origin} {s.FlightNumber} {s.BookingClass} {s.Destination}"));
            return $"{route} {fare.FareAmount:F2}NUC{fare.FareAmount:F2}END ROE1.0";
        }
    }
}