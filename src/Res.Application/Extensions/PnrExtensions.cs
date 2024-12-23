using System.Text;
using System.Text.Json;
using Res.Core.Common.Extensions;
using Res.Core.Common.Helpers;
using Res.Domain.Entities.Fare;
using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;

namespace Res.Application.Extensions
{
    public static class PnrExtensions
    {
        public static string OutputPnr(this Pnr pnr)
        {
            var sb = new StringBuilder();
            int lineNumber = 1;

            // Header line
            var officeId = pnr.Data.Agency?.OfficeId ?? "UNKNWN";
            var agentId = pnr.Data.Agency?.AgentId ?? "XX";
            var agencyCode = pnr.Data.Agency?.AgencyCode ?? "XX";
            var headerDate = pnr.Data.CreatedDate.ToAirlineFormat();

            sb.AppendLine($"RP/{officeId}/{agencyCode}                  AGENT/{agentId}       {headerDate}   {pnr.RecordLocator ?? string.Empty}");

            // Names
            foreach (var passenger in pnr.Data.Passengers)
            {
                var ticket = pnr.Data.Tickets.FirstOrDefault(t => t.PassengerId == passenger.PassengerId);
                var ticketInfo = ticket != null ? $" TKT: {ticket.TicketNumber}" : "";
                var paxType = passenger.Type.ToString().ToUpper();
                sb.AppendLine($"{lineNumber} {passenger.LastName}/{passenger.FirstName} {passenger.Title}({paxType}){ticketInfo}");
                lineNumber++;
            }

            // Segments
            foreach (var segment in pnr.Data.Segments)
            {
                if (segment.IsSurfaceSegment)
                {
                    sb.AppendLine($"{lineNumber} ARNK");
                    lineNumber++;
                    continue;
                }

                // Format segment with padding and alignment
                var segmentLine = $"{lineNumber} " +
                                  $"{segment.FlightNumber} " +
                                  $"{segment.BookingClass}  " +
                                  $"{segment.DepartureDate} " +
                                  // $"{dayOfWeek} " +
                                  $"{segment.Origin}{segment.Destination} " +
                                  $"{segment.Status.ToStatusCode()}{segment.Quantity}   " +
                                  $"{segment.DepartureTime} ";

                // Handle next day arrival with +1 or -1 if the arrival day is different to departure day

                var arrivalDateTime = DateTimeHelper.CombineDateAndTime(segment.ArrivalDate, segment.ArrivalTime);
                var departureDateTime = DateTimeHelper.CombineDateAndTime(segment.DepartureDate, segment.DepartureTime);

                int offset = (arrivalDateTime.Date - departureDateTime.Date).Days;

                if (offset != 0)
                {
                    segmentLine += $"{segment.ArrivalTime}+{offset}";
                }
                else
                {
                    segmentLine += $"{segment.ArrivalTime}";
                }

                segmentLine += " *1A/E*";
                sb.AppendLine(segmentLine);
                lineNumber++;
            }

            if (pnr.Data.SeatAssignments.Any())
            {
                foreach (var seat in pnr.Data.SeatAssignments.OrderBy(s => s.SegmentNumber))
                {
                    sb.AppendLine($"{lineNumber} ST {seat.SeatNumber} P{seat.PassengerId}/S{seat.SegmentNumber}");
                    lineNumber++;
                }
            }

            // Contact Information
            if (!string.IsNullOrEmpty(pnr.Data.Contact?.PhoneNumber))
            {
                sb.AppendLine($"{lineNumber} AP {pnr.Data.Contact.PhoneNumber}-M");
                lineNumber++;
            }
            if (!string.IsNullOrEmpty(pnr.Data.Contact?.EmailAddress))
            {
                // Format email with double slash for Amadeus style
                var emailFormatted = pnr.Data.Contact.EmailAddress.Replace("@", "//");
                sb.AppendLine($"{lineNumber} AP {emailFormatted}-H");
                lineNumber++;
            }

            // Ticketing Arrangements
            if (pnr.Data.TicketingInfo.TimeLimit != DateTime.MinValue)
            {
                sb.AppendLine($"{lineNumber} TK TL{pnr.Data.TicketingInfo.TimeLimit.ToAirlineFormat()}/");
                lineNumber++;
            }

            // OSI (Other Service Information) grouped by category
            var groupedOsis = pnr.Data.OtherServiceInformation
                .OrderBy(osi => osi.Category)
                .GroupBy(osi => osi.Category);

            foreach (var group in groupedOsis)
            {
                foreach (var osi in group)
                {
                    var segment = pnr.Data.Segments.FirstOrDefault();
                    string companyId = osi.CompanyId ?? (segment?.FlightNumber.Substring(0, 2) ?? "YY");

                    // Format for different OSI categories
                    string osiLine = group.Key switch
                    {
                        OsiCategory.FrequentFlyer => $"OSI {companyId} FQTV {osi.Text}",
                        OsiCategory.ContactInfo => $"OSI {companyId} CTCT {osi.Text}",
                        OsiCategory.Documentation => $"OSI {companyId} DOCS {osi.Text}",
                        OsiCategory.CorporateInfo => $"OSI {companyId} CORP {osi.Text}",
                        OsiCategory.PaymentInfo => $"OSI {companyId} PYMT {osi.Text}",
                        OsiCategory.GroundServices => $"OSI {companyId} GRND {osi.Text}",
                        OsiCategory.MedicalInfo => $"OSI {companyId} MEDICAL {osi.Text}",
                        OsiCategory.SecurityInfo => $"OSI {companyId} SECURITY {osi.Text}",
                        OsiCategory.OperationalInfo => $"OSI {companyId} {osi.Text}",
                        _ => $"OS {companyId} {osi.Text}"
                    };

                    sb.AppendLine($"{lineNumber} {osiLine}");
                    lineNumber++;
                }
            }

            // Special Service pnr.Data
            foreach (var ssr in pnr.Data.SpecialServiceRequests.OrderBy(s => s.Code))
            {
                var segment = pnr.Data.Segments.FirstOrDefault();
                if (segment != null)
                {
                    sb.AppendLine($"{lineNumber} SSR {ssr.Code} {segment.FlightNumber.Substring(0, 2)} HK1 {segment.FlightNumber} {segment.Origin}{segment.Destination}");
                    lineNumber++;
                }
            }

            // Remarks
            foreach (var remark in pnr.Data.Remarks)
            {
                sb.AppendLine($"{lineNumber} RM {remark}");
                lineNumber++;
            }

            // Add fare information if exists
            if (pnr.Data.Fares.Any())
            {
                foreach (var fare in pnr.Data.Fares.Where(f => f.IsStored))
                {
                    sb.AppendLine($"{lineNumber} FE {fare.FareRestrictions}");
                    lineNumber++;

                    if (!string.IsNullOrEmpty(pnr.Data.FormOfPayment))
                    {
                        var fop = pnr.Data.FormOfPayment.Split("/");

                        switch (fop[0])
                        {
                            case "CC":
                                // Mask the card details
                                sb.AppendLine($"{lineNumber} FP {PaymentHelper.MaskCreditCard(fop[1], fop[2], fop[3])} {fop[4]}");
                                break;
                            case "CA":
                                sb.AppendLine($"{lineNumber} FP CASH {fop[1]}");
                                break;
                            case "MS":
                                sb.AppendLine($"{lineNumber} FP MISC {fop[1]} {fop[2]}");
                                break;
                        }

                        lineNumber++;
                    }
                }
            }

            return sb.ToString();
        }

        public static string OutputJson(this Pnr pnr)
        {
            // Convert the PNR to JSON with proper formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(pnr, options);
        }

        public static string OutputPnrs(this List<Pnr> pnrs)
        {
            if (!pnrs.Any())
                return "NO PNRS FOUND";

            var sb = new StringBuilder();

            for (int i = 0; i < pnrs.Count; i++)
            {
                var pnr = pnrs[i];

                // Get the first passenger and first segment
                var passenger = pnr.Data.Passengers.FirstOrDefault();
                var segment = pnr.Data.Segments.FirstOrDefault();

                if (passenger == null || segment == null)
                    continue;

                // Format: LINE_NUM  NAME      DATE ROUTE STATUS  LOCATOR  STATUS
                string lineNum = $"{i + 1}".PadRight(3);
                string name = $"{passenger.LastName}/{passenger.FirstName} {passenger.Title}".PadRight(20);
                string date = segment.DepartureDate.PadRight(6);
                string time = segment.DepartureTime.PadRight(6);
                string route = $"{segment.Origin}{segment.Destination}".PadRight(8);
                string status = segment.Status.ToStatusCode().PadRight(4); ;
                int quantity = segment.Quantity;
                string locator = pnr.RecordLocator.PadRight(8);

                // Combine all fields with proper spacing
                sb.AppendLine($"{lineNum}{name}{date}{time}{route}{locator}{status}{quantity}");
            }

            return sb.ToString();
        }

        public static string OutputDocs(this Pnr currentPnr)
        {
            var sb = new StringBuilder();
            int docIndex = 1;

            sb.AppendLine("TRAVEL DOCUMENTS");
            sb.AppendLine(new string('-', 75));

            foreach (var passenger in currentPnr.Data.Passengers)
            {
                if (passenger.Documents.Any())
                {
                    sb.AppendLine($"PASSENGER {passenger.PassengerId}: {passenger.LastName}/{passenger.FirstName}");

                    foreach (var doc in passenger.Documents)
                    {
                        sb.AppendLine($"{docIndex}. TYPE: {doc.Type}");
                        sb.AppendLine($"   NUMBER: {doc.Number}");
                        sb.AppendLine($"   NATIONALITY: {doc.Nationality}");
                        sb.AppendLine($"   ISSUING COUNTRY: {doc.IssuingCountry}");
                        sb.AppendLine($"   DATE OF BIRTH: {doc.DateOfBirth:ddMMMyy}");
                        sb.AppendLine($"   GENDER: {doc.Gender}");
                        sb.AppendLine($"   EXPIRY: {doc.ExpiryDate:ddMMMyy}");

                        // Find and display associated SSR
                        var ssr = currentPnr.Data.SpecialServiceRequests
                            .FirstOrDefault(s =>
                                s.Type == SsrType.Passport &&
                                s.PassengerId == passenger.PassengerId &&
                                s.Text.Contains(doc.Number));

                        if (ssr != null)
                        {
                            sb.AppendLine($"   SSR: {ssr.Code} {ssr.Status} {ssr.ActionCode}{ssr.Quantity}");
                        }

                        sb.AppendLine(new string('-', 75));
                        docIndex++;
                    }
                }
            }

            if (docIndex == 1)
            {
                return "NO DOCUMENTS FOUND";
            }

            return sb.ToString();
        }

        public static string OutputTickets(this Pnr currentPnr)
        {
            if (!currentPnr.Data.Tickets.Any())
                return "NO TICKETS FOUND IN PNR";

            var sb = new StringBuilder();

            // Header
            sb.AppendLine("TICKET DISPLAY");
            sb.AppendLine(new string('-', 75));
            sb.AppendLine("TICKET NUMBER       PASSENGER                STATUS    ISSUE DATE   FARE");
            sb.AppendLine(new string('-', 75));

            foreach (var ticket in currentPnr.Data.Tickets)
            {
                var passenger = currentPnr.Data.Passengers.First(p => p.PassengerId == ticket.PassengerId);
                var fare = currentPnr.Data.Fares.First(f => f.PassengerId == ticket.PassengerId);

                sb.AppendLine($"{ticket.TicketNumber,-17} {passenger.LastName,-12}/{passenger.FirstName,-8} " +
                              $"{ticket.Status,-9} {ticket.IssueDate:ddMMMyy}   {fare.Currency} {ticket.FareAmount:F2}");
            }

            sb.AppendLine(new string('-', 75));
            sb.AppendLine($"TOTAL TICKETS: {currentPnr.Data.Tickets.Count}");

            return sb.ToString();
        }

        public static string OutputFareQuote(this Pnr pnr)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"FARE QUOTE RECORD - {DateTime.Now:ddMMMyy}");

            foreach (var fare in pnr.Data.Fares.Where(f => f.IsStored))
            {
                var passenger = pnr.Data.Passengers.First(p => p.PassengerId == fare.PassengerId);
                // Filter out ARNK segments
                var flightSegments = pnr.Data.Segments.Where(s => !s.IsSurfaceSegment).ToList();

                sb.AppendLine($"PAX {fare.PassengerId}   {passenger.LastName}/{passenger.FirstName}             {passenger.Type}");

                // Format route and fare calculation using only flight segments
                var route = string.Join(" ", flightSegments.Select(s => $"{s.Origin} {s.FlightNumber} {s.BookingClass} {s.Destination}"));
                sb.AppendLine($"   {route} {fare.FareAmount}NUC{fare.FareAmount}END ROE0.657534");
                sb.AppendLine($"   {fare.FareBasis}");
                sb.AppendLine($"FARE {fare.Currency}{fare.FareAmount:F2}");

                // If a fare family was selected, show its details
                if (!string.IsNullOrEmpty(fare.FareFamilyCode))
                {
                    var selectedFamily = fare.AvailableFareFamilies?.FirstOrDefault(f => f.Code == fare.FareFamilyCode);
                    if (selectedFamily != null)
                    {
                        sb.AppendLine($"FARE FAMILY: {selectedFamily.Name} ({selectedFamily.Code})");
                        sb.AppendLine($"BENEFITS:");
                        foreach (var benefit in selectedFamily.Benefits)
                        {
                            sb.AppendLine($"   * {benefit}");
                        }
                    }
                }

                // Show standard taxes
                sb.AppendLine("TX001 264.17GB TX002 88.53UB TX003 53.90US");
                sb.AppendLine($"TOTAL {fare.Currency}{fare.FareAmount + 406.60m:F2}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string OutputFareInfo(this Pnr pnr)
        {
            var sb = new StringBuilder();

            // Group passengers by type
            var passengerGroups = pnr.Data.Fares
                .GroupBy(f => pnr.Data.Passengers.First(p => p.PassengerId == f.PassengerId).Type)
                .OrderBy(g => g.Key);

            decimal totalFare = 0;

            foreach (var group in passengerGroups)
            {
                var passengerType = group.Key;
                var fareForType = group.First(); // Take first passenger's fare as representative
                var passengerCount = group.Count();

                sb.AppendLine($"\nFARE BASIS INFORMATION FOR: {passengerCount} {passengerType}");
                sb.AppendLine(new string('-', 75));

                if (fareForType.AvailableFareFamilies?.Any() == true)
                {
                    // Header for fare options
                    sb.AppendLine($"{"OPT",-4} {"FAMILY",-10} {"BASE FARE",-12} {"TOTAL",-12} {"RULES",-30}");
                    sb.AppendLine(new string('-', 75));

                    // Fare options
                    for (int i = 0; i < fareForType.AvailableFareFamilies.Count; i++)
                    {
                        var family = fareForType.AvailableFareFamilies[i];
                        var totalForFamily = family.TotalFare * passengerCount;

                        sb.AppendLine($"{i + 1,-4} {family.Name,-10} " +
                                    $"{family.BaseFare,10:F2} {fareForType.Currency} " +
                                    $"{totalForFamily,10:F2} {fareForType.Currency} " +
                                    $"{GenerateFamilyDescription(family)}");
                    }

                    // Show flight segments
                    var flightSegments = pnr.Data.Segments.Where(s => !s.IsSurfaceSegment).ToList();
                    sb.AppendLine("\nROUTING INFORMATION:");
                    foreach (var segment in flightSegments)
                    {
                        sb.AppendLine($"{segment.FlightNumber} {segment.BookingClass} {segment.Origin}{segment.Destination} " +
                                    $"{segment.DepartureDate} {segment.DepartureTime}/{segment.ArrivalTime}");
                    }

                    // Benefits for each fare family
                    sb.AppendLine("\nFARE BENEFITS BY FAMILY:");
                    foreach (var family in fareForType.AvailableFareFamilies)
                    {
                        sb.AppendLine($"\n{family.Name} ({family.Code}):");
                        foreach (var benefit in family.Benefits)
                        {
                            sb.AppendLine($"  * {benefit}");
                        }

                        // Add fare family total for this passenger type
                        decimal familyTotal = family.TotalFare * passengerCount;
                        sb.AppendLine($"TOTAL FOR {passengerCount} {passengerType}: {fareForType.Currency} {familyTotal:F2}");
                    }

                    // Add base price for cheapest option to total
                    totalFare += fareForType.AvailableFareFamilies.Min(f => f.TotalFare) * passengerCount;
                }

                // Display passenger list for this type
                sb.AppendLine($"\nPASSENGERS:");
                foreach (var fare in group)
                {
                    var passenger = pnr.Data.Passengers.First(p => p.PassengerId == fare.PassengerId);
                    sb.AppendLine($"   {passenger.PassengerId}. {passenger.LastName}/{passenger.FirstName} {passenger.Title}");
                }

                sb.AppendLine(new string('-', 75));
            }

            // Show grand total
            sb.AppendLine($"\nLOWEST TOTAL FARE: {pnr.Data.Fares.First().Currency} {totalFare:F2}");

            // Add exchange rate if applicable
            var fareWithRate = pnr.Data.Fares.FirstOrDefault(f => !string.IsNullOrEmpty(f.FareRestrictions));
            if (fareWithRate != null)
            {
                sb.AppendLine($"RATE OF EXCHANGE: {fareWithRate.FareRestrictions}");
            }

            if (pnr.Data.TicketingInfo?.TimeLimit != null)
            {
                sb.AppendLine($"LAST DATE TO TICKET: {pnr.Data.TicketingInfo.TimeLimit:ddMMMyy}");
            }

            return sb.ToString();
        }

        private static string GenerateFamilyDescription(FareFamily family)
        {
            var desc = new List<string>();

            if (family.IsRefundable)
                desc.Add("REFUNDABLE");
            if (family.IsChangeable)
            {
                if (family.ChangeFee > 0)
                    desc.Add($"CHANGE FEE {family.ChangeFee:F2}");
                else
                    desc.Add("FREE CHANGES");
            }
            else
                desc.Add("NON-CHANGEABLE");

            if (family.HasBaggageIncluded)
                desc.Add($"BAG{(family.Code == "FLEX" ? "S(2PC)" : "(1PC)")}");

            return string.Join(", ", desc);
        }

        public static string OutputFareNotes(this Pnr pnr)
        {
            var sb = new StringBuilder();

            foreach (var fare in pnr.Data.Fares.Where(f => f.IsStored))
            {
                sb.AppendLine($"01  FARE BASIS {fare.FareBasis}");
                foreach (var segment in pnr.Data.Segments)
                {
                    sb.AppendLine($"    {segment.Origin}-{segment.Destination}");
                }
                sb.AppendLine("    RESERVATIONS REQUIRED");
                sb.AppendLine($"    {fare.FareRestrictions}");
            }

            return sb.ToString();
        }

        public static string OutputFareHistory(this Pnr pnr)
        {
            var sb = new StringBuilder();

            foreach (var fare in pnr.Data.Fares.Where(f => f.IsStored))
            {
                sb.AppendLine($"01  {pnr.Data.CreatedDate:ddMMMyy HHmm}Z  STORED FARE");
                sb.AppendLine($"    AGENT {pnr.Data.Agency.AgentId}/{pnr.Data.Agency.OfficeId}");
                if (fare.LastDateToTicket.HasValue)
                {
                    sb.AppendLine($"    LAST DATE TO TICKET {fare.LastDateToTicket.Value:ddMMMyy}");
                }
            }

            return sb.ToString();
        }

        public static string OutputFareRules(this Pnr pnr)
        {
            var sb = new StringBuilder();

            foreach (var segment in pnr.Data.Segments)
            {
                sb.AppendLine($"BOOKING CLASS {segment.BookingClass}");
                sb.AppendLine("SEASON LOW");
                sb.AppendLine("MIN STAY   NONE");
                sb.AppendLine("MAX STAY   12M");
                sb.AppendLine("ADV RES    NONE");
                sb.AppendLine("PENALTIES  CHANGES GBP150.00");
                sb.AppendLine("           REFUND  NOT PERMITTED");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}