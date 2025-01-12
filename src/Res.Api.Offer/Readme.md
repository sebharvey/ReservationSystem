# Flight Search API

## Overview
This project implements a flight search API using Azure Functions and Clean Architecture principles. The API integrates with downstream inventory and faring microservices to provide consolidated flight search results including availability and pricing information.

## Architecture

### Clean Architecture Layers

#### 1. Domain Layer (`FlightSearch.Domain`)
- Contains business entities and interfaces
- Core business rules and logic
- No dependencies on external frameworks

Key Components:
- Search request/response models
- Flight and offer models
- Service interfaces

#### 2. Application Layer (`FlightSearch.Application`)
- Implements business logic
- Orchestrates domain entities
- Depends only on the Domain layer

Key Components:
- FlightSearchService
- Business logic implementations
- Data transformation logic

#### 3. Infrastructure Layer (`FlightSearch.Infrastructure`)
- Implements interfaces defined in Domain layer
- Handles external service communication
- Contains third-party service integrations

Key Components:
- InventoryService
- FaringService
- External API clients

#### 4. API Layer (`FlightSearch.Api`)
- Azure Function implementation
- Handles HTTP requests
- Manages dependency injection
- Implements CORS policies

## Getting Started

### Prerequisites
- .NET 6.0 SDK or later
- Azure Functions Core Tools
- Azure Storage Emulator (for local development)
- Visual Studio 2022 or VS Code

### Configuration
1. Update `local.settings.json` with appropriate service URLs:
```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "InventoryApi:BaseUrl": "http://inventory-service/api",
        "FaringApi:BaseUrl": "http://faring-service/api"
    }
}
```

### Building and Running

1. Clone the repository
```bash
git clone [repository-url]
cd flight-search-api
```

2. Build the solution
```bash
dotnet build
```

3. Run locally
```bash
func start
```

## API Endpoints

### Search Flights
```http
POST /api/offer/search
```

Request Body:
```json
{
    "From": "LHR",
    "To": "JFK",
    "Date": "2024-02-01",
    "Currency": "GBP",
    "Passengers": [
        {
            "Ptc": "ADT",
            "Total": 1
        }
    ]
}
```

Response:
```json
{
    "flights": [
        {
            "flightNo": "BA123",
            "aircraftType": "789",
            "departureDateTime": "2024-02-01T10:00:00",
            "arrivalDateTime": "2024-02-01T13:00:00",
            "offers": [
                {
                    "offerId": "BA123-Y-guid",
                    "bookingClass": "Y",
                    "price": 500.00
                }
            ]
        }
    ]
}
```

## Testing

### Running Tests
```bash
dotnet test
```

## Deployment

### Azure Deployment
1. Create an Azure Function App
2. Configure application settings
3. Deploy using Azure DevOps or GitHub Actions

```bash
func azure functionapp publish <FunctionAppName>
```

## Architecture Decisions

### Why Clean Architecture?
- Separation of concerns
- Independent of frameworks
- Testable business logic
- Maintainable and scalable
- Clear dependencies

### Why Azure Functions?
- Serverless architecture
- Auto-scaling capabilities
- Pay-per-use pricing
- Easy integration with Azure services
- Built-in monitoring

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## Error Handling
The API implements a global error handling strategy:
- Business logic errors return appropriate HTTP status codes
- Validation errors return 400 Bad Request
- Unhandled exceptions return 500 Internal Server Error
- All errors are logged with correlation IDs

## Monitoring and Logging
- Application Insights integration
- Structured logging using ILogger
- Performance metrics collection
- Dependency tracking

## Security
- Function-level authorization
- CORS policy implementation
- Secure configuration management
- HTTP/HTTPS endpoint support

## License
[MIT License](LICENSE)