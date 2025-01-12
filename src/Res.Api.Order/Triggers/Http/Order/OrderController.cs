using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Res.Api.Order.Application.Interfaces;

namespace Res.Api.Order.Triggers.Http.Order
{
    public class OrderController
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IOrderService _orderService;

        private readonly JsonSerializerOptions _jsonOptions;

        public OrderController(ILogger<OrderController> logger, IOrderService orderService)
        {
            _logger = logger;
            _orderService = orderService;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        [Function("Create")]
        public async Task<IActionResult> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "order/create")] HttpRequest req)
        {
            // Read request body asynchronously
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Log the raw request for debugging
            _logger.LogDebug($"Raw auth request: {requestBody}");

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is empty" });
            }

            OrderCreateRequest orderCreateRequest;
            try
            {
                orderCreateRequest = JsonSerializer.Deserialize<OrderCreateRequest>(requestBody, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse authentication request");
                return new BadRequestObjectResult(new { error = "Invalid JSON format" });
            }

            return new OkObjectResult(await _orderService.Create(orderCreateRequest));
        }

        [Function("Retrieve")]
        public async Task<IActionResult> Retrieve([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "order/retrieve")] HttpRequest req)
        {
            try
            {
                // Read and parse request
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogDebug($"Raw retrieve request: {requestBody}");

                if (string.IsNullOrEmpty(requestBody))
                {
                    return new BadRequestObjectResult(new { error = "Request body is empty" });
                }

                OrderRetrieveRequest retrieveRequest;
                try
                {
                    retrieveRequest = JsonSerializer.Deserialize<OrderRetrieveRequest>(requestBody, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse retrieve request");
                    return new BadRequestObjectResult(new { error = "Invalid JSON format" });
                }

                // Call service layer
                var response = await _orderService.Retrieve(retrieveRequest);

                // Return appropriate response based on service result
                if (!response.Success)
                {
                    return new BadRequestObjectResult(new { error = response.Message });
                }

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing retrieve request");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}