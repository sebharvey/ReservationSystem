using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Res.Microservices.Inventory.Infrastructure.Repositories;

namespace Res.Microservices.Inventory.API.Controllers
{
    public class BackgroundController
    {
        private readonly ILogger<BackgroundController> _logger;
        private readonly IFlightRepository _flightRepository;

        public BackgroundController(ILogger<BackgroundController> logger, IFlightRepository flightRepository)
        {
            _logger = logger;
            _flightRepository = flightRepository;
        }

        [Function("CleanupOldFlights")]
        public async Task Run([TimerTrigger("0 0 1 * * *")] // Runs at 1 AM every day
            TimerInfo timerInfo)
        {
            try
            {
                _logger.LogInformation("Starting flight cleanup at: {time}", DateTime.Now);

                var cutoffDate = DateTime.UtcNow.AddDays(-2); // Flights older than 2 days
                var deletedCount = await _flightRepository.DeleteOldFlights(cutoffDate);

                _logger.LogInformation("Successfully deleted {count} old flight records", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during flight cleanup");
                throw;
            }
        }
    }
}
