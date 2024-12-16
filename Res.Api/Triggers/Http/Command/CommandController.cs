using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Res.Api.Models;
using Res.Application.ReservationSystem;
using Res.Core.Interfaces;

namespace Res.Api.Triggers.Http.Command
{
    public class ApiController
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IUserService _userService;
        private readonly IReservationSystem _reservationSystem;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiController(ILogger<ApiController> logger, IUserService userService, IReservationSystem reservationSystem)
        {
            _logger = logger;
            _userService = userService;
            _reservationSystem = reservationSystem;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        [Function("Authenticate")]
        public async Task<IActionResult> Authenticate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "command/authenticate")] HttpRequest req)
        {
            try
            {
                // Read request body asynchronously
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Log the raw request for debugging
                _logger.LogDebug($"Raw auth request: {requestBody}");

                if (string.IsNullOrEmpty(requestBody))
                {
                    return new BadRequestObjectResult(new { error = "Request body is empty" });
                }

                AuthRequest authRequest;
                try
                {
                    authRequest = JsonSerializer.Deserialize<AuthRequest>(requestBody, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse authentication request");
                    return new BadRequestObjectResult(new { error = "Invalid JSON format" });
                }

                if (authRequest == null)
                {
                    return new BadRequestObjectResult(new { error = "Failed to parse request body" });
                }

                if (string.IsNullOrEmpty(authRequest?.UserId) || string.IsNullOrEmpty(authRequest?.Password))
                {
                    return new BadRequestObjectResult(new { error = "Username and password are required" });
                }

                // Authenticate user
                var authResponse = _userService.Authenticate(authRequest.UserId.ToUpper(), authRequest.Password);

                if (!authResponse.Success)
                {
                    return new UnauthorizedObjectResult(new { error = "Invalid credentials" });
                }

                // Return token
                return new OkObjectResult(new { token = authResponse.Token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function("Command")]
        public async Task<IActionResult> Command([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "command/execute")] HttpRequest req)
        {
            try
            {
                // Get bearer token
                string authHeader = req.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return new UnauthorizedObjectResult(new { error = "No bearer token provided" });
                }

                string token = authHeader.Substring("Bearer ".Length);

                // Read request body asynchronously
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Log the raw request for debugging
                _logger.LogDebug($"Raw command request: {requestBody}");

                CommandRequest commandRequest;
                try
                {
                    commandRequest = JsonSerializer.Deserialize<CommandRequest>(requestBody, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse command request");
                    return new BadRequestObjectResult(new { error = "Invalid JSON format" });
                }

                if (string.IsNullOrEmpty(commandRequest?.Command))
                {
                    return new BadRequestObjectResult(new { error = "Command is required" });
                }

                // Process command
                var result = await _reservationSystem.ProcessCommand(commandRequest.Command, token);

                if (!result.Success)
                {
                    return new BadRequestObjectResult(new { error = result.Message });
                }

                // Return command response
                return new OkObjectResult(new { data = result.Response?.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}