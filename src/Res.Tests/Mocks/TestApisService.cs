using Res.Core.Interfaces;
using Res.Domain.Entities.CheckIn;

namespace Res.Tests.Mocks
{
    public class TestApisService : IApisService
    {
        public Task<bool> ValidateApisData(ApisData apisData)
        {
            // Implement test validation logic
            return Task.FromResult(true);
        }

        public Task<bool> SubmitApisData(ApisData apisData)
        {
            // Simulate successful submission
            return Task.FromResult(true);
        }

        public Task<ApisData> BuildApisFromPnr(string recordLocator, string flightNumber)
        {
            // Return test APIS data
            return Task.FromResult(new ApisData
            {
                RecordLocator = recordLocator,
                FlightNumber = flightNumber,
                // ... other test data
            });
        }
    }
}