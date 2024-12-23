//using System.Diagnostics;
//using Res.Domain.Enums;
//using Xunit;

//namespace Res.Tests
//{
//    [Collection("Inventory")]
//    [CollectionDefinition("Inventory", DisableParallelization = true)]
//    public class ReservationSystemTests : TestBase
//    {
//        public ReservationSystemTests() : base(false) { }

//        [Fact]
//        public async Task CreateComplexPnr_WithTicketing_AndCheckIn_ShouldCreateValidPnr()
//        {
//            // Arrange
//            string pnr = string.Empty;
//            var commands = new List<string>
//            {
//                "IG",
//                "NM1DIMITRIOU/DIMITRI MR",
//                "NM1DIMITRIOU/LOUISA MRS",
//                Helpers.Common.FindFlightAndGenerateLongSellCommand(base.InventoryRepository.Inventory, "VS001", 3, "J"), // Select the next VS001 flight (specific flight)
//                Helpers.Common.FindFlightAndGenerateLongSellCommand(base.InventoryRepository.Inventory, 3, "J", "JFK", "LHR", DateTime.Today.AddDays(7).ToString("ddMMM").ToUpper()),
//                "CTCP 0727777777",
//                "TLTL08MAY",
//                "RF ABC",
//                "SR WCHR/P1/S1/NEEDS ASSISTANCE FROM GATE",
//                "SR VGML/P1",
//                "SR SPML/P1/S1/LOW SALT DIET",
//                "RM TEST REMARK 1",
//                "RM TEST REMARK 2",
//                "ET",
//                "RT{PNR}",
//                //"ST/1D/P1/S1",   // Seat allocations
//                //"ST/1D/P1/S2",   // Seat allocations
//                //"ST/1A/P2/S1",   // Seat allocations
//                //"ST/1A/P2/S2",   // Seat allocations
//                "FXP",
//                "FS",
//                "FP*CC/VISA/4444333322221111/0625/GBP892.00",
//                "TTP",
//                "SRDOCS HK1/P/GBR/P12345678/GBR/12JUL1982/M/20NOV2025/DIMITRIOU/DIMITRI", // Add passport doc for first pax  - required for APIS info
//                "SRDOCS HK1/P/GBR/P32434338/GBR/01JAN1982/M/20NOV2025/DIMITRIOU/LOUISA", // Add passport doc for second pax - required for APIS info
//                "SRDOCA HK1/P1/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA", // Add address info for first pax  - required for APIS info
//                "SRDOCA HK1/P2/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA", // Add address info for second pax - required for APIS info
//                "ET",
//                "CKIN {PNR}/P1/VS001/1A",
//                "CKIN {PNR}/P2/VS001/1D",
//                "IG"
//            };

//            // Act
//            foreach (var command in commands)
//            {
//                var cmd = command.Replace("{PNR}", pnr);

//                var result = await ReservationSystem.ProcessCommand(cmd, Token);

//                Debug.WriteLine($"CMD>{cmd} > {result.Response}");
//                Debug.WriteLine($"{result.Response}");

//                if (command == "ET")
//                {
//                    if (!result.Success)
//                    {
//                        throw new InvalidOperationException(result.Response.ToString());
//                    }

//                    // Extract PNR locator from ER response
//                    pnr = result.Response.ToString()!.Substring(5, 6);
//                }
//            }

//            // Retrieve the final PNR
//            var retrievedPnr = await ReservationService.RetrievePnr(pnr);

//            // Assert
//            Assert.NotNull(retrievedPnr);

//            // Passenger assertions
//            Assert.Equal(2, retrievedPnr.Passengers.Count);
//            var pax1 = Assert.Single(retrievedPnr.Passengers.Where(p => p.FirstName == "DIMITRI"));
//            var pax2 = Assert.Single(retrievedPnr.Passengers.Where(p => p.FirstName == "LOUISA"));
//            Assert.Equal("DIMITRIOU", pax1.LastName);
//            Assert.Equal("MR", pax1.Title);
//            Assert.Equal("DIMITRIOU", pax2.LastName);
//            Assert.Equal("MRS", pax2.Title);

//            // Segment assertions
//            Assert.Equal(2, retrievedPnr.Segments.Count);
//            var outbound = retrievedPnr.Segments[0];
//            var inbound = retrievedPnr.Segments[1];

//            Assert.Equal("VS001", outbound.FlightNumber);
//            Assert.Equal("J", outbound.BookingClass);
//            Assert.Equal("LHR", outbound.Origin);
//            Assert.Equal("JFK", outbound.Destination);
//            Assert.Equal(SegmentStatus.Confirmed, outbound.Status);

//            Assert.NotEmpty(inbound.FlightNumber);
//            Assert.Equal("J", inbound.BookingClass);
//            Assert.Equal("JFK", inbound.Origin);
//            Assert.Equal("LHR", inbound.Destination);
//            Assert.Equal(SegmentStatus.Confirmed, inbound.Status);

//            // Contact info assertions
//            Assert.NotNull(retrievedPnr.Contact);
//            Assert.Equal("0727777777", retrievedPnr.Contact.PhoneNumber);

//            // SSR assertions
//            Assert.Contains(retrievedPnr.SpecialServiceRequests, s => s.Code == "WCHR");
//            Assert.Contains(retrievedPnr.SpecialServiceRequests, s => s.Code == "VGML");
//            Assert.Contains(retrievedPnr.SpecialServiceRequests, s => s.Code == "SPML");
//            Assert.Contains(retrievedPnr.SpecialServiceRequests, s => s.Code == "DOCS" && s.Text.Contains("P12345678"));
//            Assert.Contains(retrievedPnr.SpecialServiceRequests, s => s.Code == "DOCS" && s.Text.Contains("P32434338"));

//            // Document assertions
//            var pax1Docs = pax1.Documents;
//            var pax2Docs = pax2.Documents;
//            Assert.Single(pax1Docs);
//            Assert.Single(pax2Docs);
//            Assert.Equal("P12345678", pax1Docs[0].Number);
//            Assert.Equal("P32434338", pax2Docs[0].Number);

//            // Remark assertions
//            Assert.Contains("TEST REMARK 1", retrievedPnr.Remarks);
//            Assert.Contains("TEST REMARK 2", retrievedPnr.Remarks);

//            // Ticketing assertions
//            Assert.NotEmpty(retrievedPnr.Tickets);
//            foreach (var ticket in retrievedPnr.Tickets)
//            {
//                Assert.Equal(TicketStatus.Valid, ticket.Status);
//                Assert.Equal(2, ticket.Coupons.Count);

//                var coupon = ticket.Coupons.FirstOrDefault(item => item.FlightNumber == "VS001");

//                Assert.NotNull(coupon);
//                Assert.Equal(CouponStatus.CheckedIn, coupon.Status);
//            }

//            // Check-in assertions
//            var checkInOsis = retrievedPnr.OtherServiceInformation
//                .Where(o => o.Category == OsiCategory.OperationalInfo && o.Text.StartsWith("CKIN"))
//                .ToList();

//            Assert.Equal(2, checkInOsis.Count);
//            Assert.Contains(checkInOsis, o => o.Text.Contains("1A"));
//            Assert.Contains(checkInOsis, o => o.Text.Contains("1D"));

//            // PNR status assertions
//            Assert.Equal(PnrStatus.Ticketed, retrievedPnr.Status);
//            Assert.NotNull(retrievedPnr.TicketingInfo.TimeLimit);
//            Assert.Equal(new DateTime(2024, 5, 8), retrievedPnr.TicketingInfo.TimeLimit.Date);
//        }

//        [Fact]
//        public async Task CreatePnr_WithRandomSeatAtCheckin()
//        {
//            // Arrange
//            string pnr = string.Empty;
//            var commands = new List<string>
//            {
//                "IG",
//                "NM1DIMITRIOU/DIMITRI MR",
//                "NM1DIMITRIOU/LOUISA MRS",
//                Helpers.Common.FindFlightAndGenerateLongSellCommand(InventoryRepository.Inventory, "VS001", 3, "J"), // Select the next VS001 flight (specific flight)
//                Helpers.Common.FindFlightAndGenerateLongSellCommand(InventoryRepository.Inventory, 3, "J", "JFK", "LHR", DateTime.Today.AddDays(7).ToString("ddMMM").ToUpper()),
//                "CTCP 0727777777",
//                "TLTL08MAY",
//                "RF ABC",
//                "SR WCHR/P1/S1/NEEDS ASSISTANCE FROM GATE",
//                "SR VGML/P1",
//                "SR SPML/P1/S1/LOW SALT DIET",
//                "RM TEST REMARK 1",
//                "RM TEST REMARK 2",
//                "ET",
//                "RT{PNR}",
//                "FXP",
//                "FS",
//                "FP*CC/VISA/4444333322221111/0625/GBP892.00",
//                "TTP",
//                "SRDOCS HK1/P/GBR/P12345678/GBR/12JUL1982/M/20NOV2025/DIMITRIOU/DIMITRI", // Add passport doc for first pax  - required for APIS info
//                "SRDOCS HK1/P/GBR/P32434338/GBR/01JAN1982/M/20NOV2025/DIMITRIOU/LOUISA", // Add passport doc for second pax - required for APIS info
//                "SRDOCA HK1/P1/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA", // Add address info for first pax  - required for APIS info
//                "SRDOCA HK1/P2/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA", // Add address info for second pax - required for APIS info
//                "ET",
//                "CKIN {PNR}/P1/VS001",
//                "CKIN {PNR}/P2/VS001",
//                "IG"
//            };

//            // Act
//            foreach (var command in commands)
//            {
//                var cmd = command.Replace("{PNR}", pnr);

//                var result = await ReservationSystem.ProcessCommand(cmd, Token);

//                Debug.WriteLine($"CMD>{cmd} > {result.Response}");
//                Debug.WriteLine($"{result.Response}");

//                if (command == "ET")
//                {
//                    if (!result.Success)
//                    {
//                        throw new InvalidOperationException(result.Response.ToString());
//                    }

//                    // Extract PNR locator from ER response
//                    pnr = result.Response.ToString()!.Substring(5, 6);
//                }
//            }

//            // Retrieve the final PNR
//            var retrievedPnr = await ReservationService.RetrievePnr(pnr);

//            // Assert
//            Assert.NotNull(retrievedPnr);

//            // Check-in assertions
//            var checkInOsis = retrievedPnr.OtherServiceInformation
//                .Where(o => o.Category == OsiCategory.OperationalInfo && o.Text.StartsWith("CKIN"))
//                .ToList();

//            Assert.Equal(2, checkInOsis.Count);
//            Assert.True(!string.IsNullOrWhiteSpace(checkInOsis[0].Text));
//            Assert.True(!string.IsNullOrWhiteSpace(checkInOsis[1].Text));
//        }

//    }
//}