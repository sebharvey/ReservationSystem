using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Res.Api.Order.Application.Interfaces;

namespace Res.Api.Order.Triggers.Http.Order
{
    public class SeatController
    {
        private readonly ILogger<SeatController> _logger;
        private readonly ISeatService _seatService;

        private readonly JsonSerializerOptions _jsonOptions;

        public SeatController(ILogger<SeatController> logger, ISeatService seatService)
        {
            _logger = logger;
            _seatService = seatService;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        [Function("ViewSeatMap")]
        public async Task<IActionResult> ViewSeatMap([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "seat/seatmap")] HttpRequest req)
        {
            try
            {
                // Read and validate request
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogDebug("Received seatmap request: {RequestBody}", requestBody);

                if (string.IsNullOrEmpty(requestBody))
                {
                    return new BadRequestObjectResult(new { error = "Request body is required" });
                }

                // Parse request
                SeatMapRequest seatMapRequest;
                try
                {
                    seatMapRequest = JsonSerializer.Deserialize<SeatMapRequest>(requestBody, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse seatmap request");
                    return new BadRequestObjectResult(new { error = "Invalid request format" });
                }

                // Validate request fields
                if (string.IsNullOrEmpty(seatMapRequest?.FlightNumber))
                {
                    return new BadRequestObjectResult(new { error = "Flight number is required" });
                }

                if (seatMapRequest.DepartureDate == default)
                {
                    return new BadRequestObjectResult(new { error = "Valid departure date is required" });
                }

                // Validate departure date is not in the past
                if (seatMapRequest.DepartureDate.Date < DateTime.Today)
                {
                    return new BadRequestObjectResult(new { error = "Departure date cannot be in the past" });
                }

                // Get seatmap from service
                var response = await _seatService.ViewSeatMap(seatMapRequest);

                // Return appropriate response
                if (!response.Success)
                {
                    return new BadRequestObjectResult(new { error = response.Message });
                }

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing seatmap request");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
