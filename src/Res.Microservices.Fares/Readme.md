# Airline Dynamic Pricing API

An Azure Function-based API that provides dynamic pricing recommendations for airline flights using AI-powered analysis. The API considers multiple factors including cabin inventory, time of day, seasonality, and passenger mix to generate competitive pricing.

## Solution Architecture

The solution follows Clean Architecture principles with clear separation of concerns:

### Project Structure
```
AirlinePricing/
├── AirlinePricing.Domain/           # Core domain entities and business rules
│   ├── Entities/                    # Domain entities (Flight, Passenger, etc.)
│   └── ValueObjects/                # Value objects (PricingFactors, etc.)
│
├── AirlinePricing.Application/      # Application business logic
│   ├── DTOs/                        # Data Transfer Objects
│   ├── Interfaces/                  # Core interfaces
│   └── Services/                    # Application services
│
├── AirlinePricing.Infrastructure/   # External concerns implementation
│   ├── Services/                    # External service implementations
│   └── Configuration/               # Infrastructure configuration
│
└── AirlinePricing.Api/             # API layer
    ├── Functions/                   # Azure Functions
    └── Startup.cs                   # DI and configuration setup
```

### Namespaces Overview

#### AirlinePricing.Domain
Contains the core business model and logic:
- `Entities`: Core business objects (Flight, Passenger)
- `ValueObjects`: Immutable objects representing concepts like pricing factors
- No dependencies on external libraries or services

#### AirlinePricing.Application
Contains business logic and interfaces:
- `DTOs`: Data transfer objects for input/output
- `Interfaces`: Core service contracts
- `Services`: Business logic implementation
- Depends only on the Domain layer

#### AirlinePricing.Infrastructure
Implements interfaces defined in the Application layer:
- `Services`: Implementation of external service clients
- `Configuration`: Infrastructure configuration classes
- Contains Claude API client and mock implementations

#### AirlinePricing.Api
Contains the Azure Function and configuration:
- `Functions`: HTTP-triggered functions
- Handles dependency injection and configuration
- Entry point for the application

## Development and Testing Mode

The API supports two operational modes:
- Production: Uses the real Claude API for pricing recommendations
- Development: Uses mock responses for testing and development

### Configuring Mock Mode

1. **Local Development (local.settings.json)**
```json
{
  "IsEncrypted": false,
  "Values": {
    "ServiceSettings__Mode": "Development",
    "ServiceSettings__AnthropicApiKey": "your-api-key-here"
  }
}
```

2. **Azure Configuration**
Add these application settings in your Azure Function App:
```
ServiceSettings__Mode: "Development" or "Production"
ServiceSettings__AnthropicApiKey: "your-api-key"
```

### Mock Service Features

The mock service provides realistic responses with:
- Time-based pricing variations
- Cabin-class appropriate fares
- Occupancy-based price adjustments
- Seasonal variations
- Realistic tax calculations
- Dynamic demand levels

Mock responses maintain consistent pricing patterns:
- Business Class (J) > Premium Economy (W) > Economy (Y)
- Time of day adjustments:
  - Morning: Standard rates
  - Afternoon: 10% discount
  - Evening: 5% discount
  - Night: 15% discount
- Occupancy impacts:
  - >80%: 20% premium
  - >60%: 10% premium
  - >40%: Standard rates
  - <40%: 10% discount

## API Endpoint

```
POST https://{your-function-app}.azurewebsites.net/api/GetMultiFlightPricing
```

[Previous documentation on authentication, request format, etc. remains the same...]

## Request Format

```json
{
    "origin": "string",           # 3-letter airport code
    "destination": "string",      # 3-letter airport code
    "travelDate": "string",       # ISO 8601 date format
    "flights": [
        {
            "flightNumber": "string",
            "departureTime": "string",    # Format: "HH:mm:ss"
            "cabinInventory": {
                "J": {                    # Business Class
                    "totalSeats": number,
                    "availableSeats": number
                },
                "W": {                    # Premium Economy
                    "totalSeats": number,
                    "availableSeats": number
                },
                "Y": {                    # Economy
                    "totalSeats": number,
                    "availableSeats": number
                }
            }
        }
    ],
    "passengers": [
        {
            "ptcCode": "string",    # Passenger Type Code (e.g., "ADT", "CHD")
            "quantity": number
        }
    ]
}
```

[Previous sections on Response Format, Pricing Factors, etc. remain the same...]

## Development Guidelines

1. **Using Mock Mode**
   - Set `ServiceSettings__Mode` to "Development"
   - No Claude API key required
   - Responses are immediate and deterministic
   - Great for UI development and testing

2. **Testing Different Scenarios**
   - Vary occupancy levels to test pricing dynamics
   - Test different times of day
   - Mix passenger types
   - Try different cabin classes

3. **Switching to Production**
   - Set `ServiceSettings__Mode` to "Production"
   - Ensure valid Claude API key is configured
   - Remove any test-specific configurations

4. **Best Practices**
   - Use mock mode for development and testing
   - Use production mode for final testing and production
   - Cache responses when possible
   - Include all relevant passenger types in a single request
   - Provide accurate cabin inventory data

## Error Handling

The API returns appropriate HTTP status codes:
- 200: Successful request
- 400: Bad request (invalid input)
- 401: Unauthorized (invalid or missing API key)
- 500: Internal server error

Error responses include a message explaining the error:
```json
{
    "error": "string"
}
```

## Caching

- In-memory caching for 5 minutes
- Cached based on route, date, flight, cabin inventory, and passengers
- Cache invalidated when inventory changes
- Works in both mock and production modes

## Rate Limiting

- Default: 20 requests per minute
- Cached responses don't count towards the limit
- Consider implementing client-side caching for frequent requests

## Setup and Deployment

1. Create an Azure Function App
2. Configure application settings:
   ```
   ServiceSettings__Mode: "Production" or "Development"
   ServiceSettings__AnthropicApiKey: "your-anthropic-api-key"
   ```
3. Deploy the function code
4. Configure CORS if needed
5. Test the endpoint with a sample request

## Support

For issues and feature requests, please contact the development team or create an issue in the repository.