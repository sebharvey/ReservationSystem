using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Res.Api.Core.Services;
using Res.Api.Models;

namespace Res.Api.Triggers.Http.Delivery
{
    public class CheckInController
    {
        private readonly ILogger<CheckInController> _logger;
        private readonly ICheckinService _checkinService;

        private readonly JsonSerializerOptions _jsonOptions;

        public CheckInController(ILogger<CheckInController> logger, ICheckinService checkinService)
        {
            _logger = logger;
            _checkinService = checkinService;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        [Function("Validate")]
        public async Task<IActionResult> Validate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "checkin/validate")] HttpRequest req)
        {
            // Read request body asynchronously
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Log the raw request for debugging
            _logger.LogDebug($"Raw auth request: {requestBody}");

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is empty" });
            }

            ValidateCheckInRequest validatCheckInRequest;
            try
            {
                validatCheckInRequest = JsonSerializer.Deserialize<ValidateCheckInRequest>(requestBody, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse authentication request");
                return new BadRequestObjectResult(new { error = "Invalid JSON format" });
            }

            return new OkObjectResult(await _checkinService.ValidateCheckin(validatCheckInRequest));
        }


        [Function("CheckIn")]
        public async Task<IActionResult> CheckIn([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "checkin/checkin")] HttpRequest req)
        {
            // Read request body asynchronously
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Log the raw request for debugging
            _logger.LogDebug($"Raw auth request: {requestBody}");

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is empty" });
            }

            CheckInRequest checkInRequest;
            try
            {
                checkInRequest = JsonSerializer.Deserialize<CheckInRequest>(requestBody, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse authentication request");
                return new BadRequestObjectResult(new { error = "Invalid JSON format" });
            }

            return new OkObjectResult(await _checkinService.CheckIn(checkInRequest));
        }
    }
}