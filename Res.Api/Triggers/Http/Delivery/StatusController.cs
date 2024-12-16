using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Res.Api.Core.Services;

namespace Res.Api.Triggers.Http.Delivery
{
    public class StatusController
    {
        private readonly ILogger<StatusController> _logger;
        private readonly IStatusService _statusService;

        private readonly JsonSerializerOptions _jsonOptions;

        public StatusController(ILogger<StatusController> logger, IStatusService statusService)
        {
            _logger = logger;
            _statusService = statusService;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter()  // This will serialize enums as strings
                }
            };
        }

        [Function("FlightStatus")]
        public async Task<IActionResult> FlightStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/flights")] HttpRequest req)
        {
            var flights = await _statusService.FlightStatus(DateTime.Now);

            // Convert to anonymous type with status as string
            var response = flights.Select(f => new
            {
                f.FlightNumber,
                f.From,
                f.To,
                f.Aircraft,
                f.DepartureDateTime,
                f.ArrivalDateTime,
                Status = f.Status.ToString()
            });

            return new OkObjectResult(response);
        }
    }
}
