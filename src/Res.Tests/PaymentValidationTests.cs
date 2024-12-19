using System.Diagnostics;
using Xunit;

namespace Res.Tests
{
    [Collection("Inventory")]
    [CollectionDefinition("Inventory", DisableParallelization = true)]

    public class PaymentValidationTests : TestBase
    {
        public PaymentValidationTests() : base(false) { }
         
        [Theory]
        [InlineData("4532015112830366")] // Valid Visa
        [InlineData("4916338506082832")] // Valid Visa
        [InlineData("5109121617551463")] // Valid Mastercard
        [InlineData("371449635398431")]  // Valid Amex
        public async Task ValidCreditCards_ShouldAcceptPayment(string cardNumber)
        {
            // Arrange
            await CreateTestPnr();

            // Act
            var result = await ReservationSystem.ProcessCommand(
                $"FP*CC/VISA/{cardNumber}/0625/GBP892.00",
                Token
            );

            // Assert
            Assert.True(result.Success);
            Assert.Equal("FORM OF PAYMENT ADDED", result.Response);
        }

        [Theory]
        [InlineData("1234567890123456")] // Fails Luhn check
        [InlineData("4532015112830367")] // Invalid check digit
        [InlineData("0000000000000000")] // All zeros
        [InlineData("9999999999999999")] // All nines
        public async Task InvalidCreditCards_ShouldRejectPayment(string cardNumber)
        {
            // Arrange
            await CreateTestPnr();

            // Act
            var result = await ReservationSystem.ProcessCommand(
                $"FP*CC/VISA/{cardNumber}/0625/GBP892.00",
                Token
            );

            // Assert
            Assert.True(result.Success);
            Assert.Equal("INVALID CARD", result.Response);
        }

        private async Task CreateTestPnr()
        {
            var commands = new[]
            {
                "IG",
                "NM1SMITH/JOHN MR",
                Helpers.Common.FindFlightAndGenerateLongSellCommand(base.InventoryRepository.Inventory, "VS001", 3, "J"), // Select the next VS001 flight (specific flight)
                "CTCP 0777777777",
                "TLTL08MAY",
                "FXP",
                "FS"
            };

            foreach (var command in commands)
            {
                try
                {
                    var result = await ReservationSystem.ProcessCommand(command, Token);
                    Debug.WriteLine($"Command: {command}");
                    Debug.WriteLine($"Result: {result.Message}");
                    if (!result.Success)
                    {
                        throw new Exception($"Command failed: {command} - {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed executing command {command}: {ex}");
                    throw;
                }
            }
        }
    }
}