//using Res.Tests.Helpers;
//using Xunit;

//namespace Res.Tests
//{
//    [Collection("Inventory")]
//    [CollectionDefinition("Inventory", DisableParallelization = true)]
//    public class SeatMapTests : TestBase
//    {
//        public SeatMapTests() : base(false) { }

//        [Fact]
//        public async Task DisplaySeatMap_ForSpecificFlight_ShouldShowSeatMap()
//        {
//            string date = Common.ToAirlineShortDate(DateTime.Now.AddDays(1));
//            // Act
//            var result = await ReservationSystem.ProcessCommand(
//                $"SM VS001/{date}",
//                Token
//            );

//            // Assert
//            Assert.True(result.Success);
//            //Assert.Contains($"SEAT MAP FOR VS001 {date}", result.Response.ToString());
//            //Assert.Contains("AIRCRAFT: 333", result.Response.ToString());
//            Assert.Contains("Upper Class (J)", result.Response.ToString());
//            Assert.Contains("Premium (W)", result.Response.ToString());
//            Assert.Contains("Economy (Y)", result.Response.ToString());
//        }

//        [Theory]
//        [InlineData("J", "4D", "1A")] // Business class seats
//        [InlineData("W", "20A", "20C")] // Premium Economy seats
//        [InlineData("Y", "31F", "31E")] // Economy seats
//        public async Task AssignSeat_WithValidPnr_ShouldAssignSeats(string bookingClass, string seat1, string seat2)
//        {
//            // Arrange
//            string pnr = string.Empty;
//            var commands = new[]
//            {
//                "IG",
//                "NM1SMITH/JOHN MR",
//                "NM1SMITH/JANE MRS",
//                Helpers.Common.FindFlightAndGenerateLongSellCommand(base.InventoryRepository.Inventory, "VS001", 2, bookingClass),
//                "CTCP 0777777777",
//                $"TLTL{Common.ToAirlineShortDate(DateTime.Now.AddDays(1))}",
//                "ER",
//                "RT{PNR}",
//                $"ST/{seat1}/P1/S1",
//                $"ST/{seat2}/P2/S1",
//                "ER"
//            };

//            // Act
//            foreach (var command in commands)
//            {
//                var cmd = command.Replace("{PNR}", pnr);
//                var result = await ReservationSystem.ProcessCommand(cmd, Token);

//                if (command == "ER")
//                {
//                    if (result.Response.ToString()!.StartsWith("OK - "))
//                    {
//                        pnr = result.Response.ToString()!.Substring(5, 6);
//                    }
//                }
//                Assert.True(result.Success);
//            }

//            // Verify seat assignments in PNR
//            var retrievedPnr = await ReservationService.RetrievePnr(pnr);

//            Assert.NotNull(retrievedPnr);
//            Assert.Equal(2, retrievedPnr.Data.SeatAssignments.Count);
//            Assert.Contains(retrievedPnr.Data.SeatAssignments, s => s.PassengerId == 1 && s.SeatNumber == seat1);
//            Assert.Contains(retrievedPnr.Data.SeatAssignments, s => s.PassengerId == 2 && s.SeatNumber == seat2);
//        }

//        [Fact]
//        public async Task DisplaySeatMap_WithinPnr_ShouldShowSeatMap()
//        {
//            // Arrange
//            string pnr = string.Empty;
//            var commands = new[]
//            {
//                "IG",
//                "NM1SMITH/JOHN MR",
//                Helpers.Common.FindFlightAndGenerateLongSellCommand(base.InventoryRepository.Inventory, "VS001", 1, "J"),
//                "CTCP 0777777777",
//                $"TLTL{Common.ToAirlineShortDate(DateTime.Now.AddDays(1))}",
//                "ER",
//                "RT{PNR}",
//                "SM1"  // Display seat map for segment 1
//            };

//            // Act & Assert
//            foreach (var command in commands)
//            {
//                var cmd = command.Replace("{PNR}", pnr);
//                var result = await ReservationSystem.ProcessCommand(cmd, Token);

//                if (command == "ER" && result.Response.ToString()!.StartsWith("OK - "))
//                {
//                    pnr = result.Response.ToString()!.Substring(5, 6);
//                }

//                Assert.True(result.Success);
//                if (command == "SM1")
//                {
//                    Assert.Contains("SEAT MAP FOR VS001", result.Response.ToString());
//                    Assert.Contains("Upper Class (J)", result.Response.ToString());
//                }
//            }
//        }

//        [Fact]
//        public async Task RemoveSeatAssignment_ShouldRemoveSeat()
//        {
//            // Arrange
//            string pnr = string.Empty;
//            var commands = new[]
//            {
//                "IG",
//                "NM1SMITH/JOHN MR",
//                Helpers.Common.FindFlightAndGenerateLongSellCommand(base.InventoryRepository.Inventory, "VS001", 1, "J"),
//                "CTCP 0777777777",
//                $"TLTL{Common.ToAirlineShortDate(DateTime.Now.AddDays(1))}",
//                "ER",
//                "RT{PNR}",
//                "ST/4D/P1/S1",  // Assign seat
//                "ER",
//                "RT{PNR}",
//                "STX/P1/S1",    // Remove seat
//                "ER"
//            };

//            // Act & Assert
//            foreach (var command in commands)
//            {
//                var cmd = command.Replace("{PNR}", pnr);
//                var result = await ReservationSystem.ProcessCommand(cmd, Token);

//                if (command == "ER" && result.Response.ToString()!.StartsWith("OK - "))
//                {
//                    pnr = result.Response.ToString()!.Substring(5, 6);
//                }

//                Assert.True(result.Success);
//            }

//            // Verify seat assignment is removed
//            var retrievedPnr = await ReservationService.RetrievePnr(pnr);
//            Assert.NotNull(retrievedPnr);
//            Assert.Empty(retrievedPnr.Data.SeatAssignments);
//        }

//        [Theory]
//        [InlineData("99A")]  // Non-existent seat
//        [InlineData("1Z")]   // Invalid seat letter
//        [InlineData("0A")]   // Invalid row number
//        public async Task AssignSeat_WithInvalidSeat_ShouldFail(string invalidSeat)
//        {
//            // Arrange
//            string pnr = string.Empty;
//            var setupCommands = new[]
//            {
//                "IG",
//                "NM1SMITH/JOHN MR",
//                Helpers.Common.FindFlightAndGenerateLongSellCommand(base.InventoryRepository.Inventory, "VS001", 1, "J"),
//                "CTCP 0777777777",
//                $"TLTL{Common.ToAirlineShortDate(DateTime.Now.AddDays(1))}",
//                "ER",
//                "RT{PNR}",
//            };

//            // Setup PNR
//            foreach (var command in setupCommands)
//            {
//                var cmd = command.Replace("{PNR}", pnr);
//                var result = await ReservationSystem.ProcessCommand(cmd, Token);

//                if (command == "ER" && result.Response.ToString()!.StartsWith("OK - "))
//                {
//                    pnr = result.Response.ToString()!.Substring(5, 6);
//                }
//            }

//            // Act
//            var result1 = await ReservationSystem.ProcessCommand($"ST/{invalidSeat}/P1/S1", Token);

//            var retrievedPnr = await ReservationService.RetrievePnr(pnr);

//            // Assert
//            Assert.NotNull(retrievedPnr);
//            Assert.Contains("INVALID SEAT NUMBER", result1.Response.ToString(), StringComparison.InvariantCultureIgnoreCase);
//        }

//        [Fact]
//        public async Task AssignSeat_WithIncorrectCabinClass_ShouldFail()
//        {
//            // Arrange
//            string pnr = string.Empty;
//            var commands = new[]
//            {
//                "IG",
//                "NM1SMITH/JOHN MR",
//                Helpers.Common.FindFlightAndGenerateLongSellCommand(base.InventoryRepository.Inventory, "VS001", 1, "Y"), // Economy booking
//                "CTCP 0777777777",
//                $"TLTL{Common.ToAirlineShortDate(DateTime.Now.AddDays(1))}",
//                "ER",
//                "RT{PNR}",
//                "ST/4D/P1/S1",  // Try to assign business class seat
//                "ER"
//            };

//            // Act & Assert
//            foreach (var command in commands)
//            {
//                var cmd = command.Replace("{PNR}", pnr);
//                var result = await ReservationSystem.ProcessCommand(cmd, Token);

//                if (command == "ER" && result.Response.ToString()!.StartsWith("OK - "))
//                {
//                    pnr = result.Response.ToString()!.Substring(5, 6);
//                }

//                if (command.StartsWith("ST/"))
//                {
//                    Assert.Contains("UNABLE TO ASSIGN SEAT", result.Response.ToString());
//                }
//            }
//        }
//    }
//}