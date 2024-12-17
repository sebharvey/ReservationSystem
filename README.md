# Airline Reservation System

A comprehensive airline reservation system that provides both a traditional cryptic command interface (similar to Amadeus/Sabre) and a modern REST API. The system handles flight bookings, check-in, seat assignments, fare management, and more.

## Features

- Flight search and availability
- Passenger Name Record (PNR) management
- Fare pricing and ticketing
- Seat maps and seat assignments
- Check-in and boarding pass generation
- APIS (Advanced Passenger Information System) handling
- Flight status tracking
- Payment processing

## Architecture Overview

```mermaid
graph TB
    subgraph "Client Layer"
        CLI[Command Line Interface]
        REST[REST API Clients]
    end

    subgraph "API Layer"
        CMD[Command Controller]
        OFR[Offer Controller]
        ORD[Order Controller]
        CHK[Check-in Controller]
        SEAT[Seat Controller]
    end

    subgraph "Application Layer"
        RS[Reservation System]
        RC[Reservation Commands]
        PNR[PNR Service]
        INV[Inventory Service]
        FARE[Fare Service]
        TKT[Ticketing Service]
        SEAT_SVC[Seat Service]
        CHK_SVC[Check-in Service]
    end

    subgraph "Domain Layer"
        PNR_ENT[PNR]
        INV_ENT[Inventory]
        FARE_ENT[Fares]
        TKT_ENT[Tickets]
        SEAT_ENT[Seat Maps]
    end

    subgraph "Infrastructure Layer"
        PNR_REPO[PNR Repository]
        INV_REPO[Inventory Repository]
        FARE_REPO[Fare Repository]
        AUTH[Authentication]
        PAY[Payment Service]
        APIS[APIS Service]
    end

    %% Client to API connections
    CLI --> CMD
    REST --> OFR & ORD & CHK & SEAT

    %% API to Application connections
    CMD --> RS
    OFR --> RS
    ORD --> RS
    CHK --> CHK_SVC
    SEAT --> SEAT_SVC

    %% Application interconnections
    RS --> RC
    RC --> PNR & INV & FARE & TKT & SEAT_SVC & CHK_SVC

    %% Application to Domain connections
    PNR --> PNR_ENT
    INV --> INV_ENT
    FARE --> FARE_ENT
    TKT --> TKT_ENT
    SEAT_SVC --> SEAT_ENT

    %% Domain to Infrastructure connections
    PNR_ENT --> PNR_REPO
    INV_ENT --> INV_REPO
    FARE_ENT --> FARE_REPO
    
    %% External service connections
    RC -.-> AUTH
    RC -.-> PAY
    CHK_SVC -.-> APIS
```

The system follows a layered architecture pattern:

- **Client Layer**: Handles user interactions through both command-line and REST interfaces
- **API Layer**: Exposes functionality through controllers for different business domains
- **Application Layer**: Contains core business logic and service implementations
- **Domain Layer**: Defines the business entities and rules
- **Infrastructure Layer**: Manages data persistence and external service integrations

## Architecture

The solution is organized into several key projects:

### Core Projects

- **Res.Api**: REST API endpoints and controllers
- **Res.Application**: Business logic and command processing
- **Res.Core**: Core domain services and interfaces
- **Res.Domain**: Domain entities, DTOs, and enums
- **Res.Infrastructure**: Data persistence and external service integrations

### Domain Model

- **PNR**: Central booking record containing passenger, itinerary, and ticketing info
- **Inventory**: Flight schedules and seat availability
- **Fare**: Pricing rules and fare families
- **Ticket**: E-ticket records and flight coupons
- **CheckIn**: Boarding passes and passenger processing

## Command Interface

The system supports traditional airline cryptic commands. Here are some key commands:

### Availability and Booking

```
AN20JUNLHRJFK/1400    # Search flights LHR-JFK on 20JUN at 14:00
SS1Y2                 # Sell 1 seat in Y class from line 2
NM1SMITH/JOHN MR      # Add passenger name
CTCP 44123456789      # Add phone contact
CTCE TEST@EMAIL.COM   # Add email contact
ER                    # End transaction and retrieve
```

### Pricing and Ticketing

```
FXP                   # Price PNR
FS                    # Store fare
FP*CC/VISA/4444333322221111/0625/GBP100.00  # Add credit card payment
TTP                   # Issue tickets
```

### Seat Assignment

```
SM1                   # Display seat map for segment 1
ST/4A/P1/S1          # Assign seat 4A to passenger 1 segment 1
```

### Check-in

```
CKIN ABC123/P1/VS001/12A     # Check in passenger 1 on VS001, seat 12A
CKINALL ABC123/VS001         # Check in all passengers
```

## REST API

The system also provides a modern REST API:

### Authentication

```http
POST /api/command/authenticate
{
    "userId": "AGENT1",
    "password": "password123"
}
```

### Flight Search

```http
POST /api/offer/search
{
    "from": "LHR",
    "to": "JFK",
    "date": "2024-06-20",
    "currency": "GBP",
    "passengers": [
        {
            "ptc": "ADT",
            "total": 1
        }
    ]
}
```

### Create Booking

```http
POST /api/order/create
{
    "outboundOfferId": "VS001Y20JUNLHRJFK",
    "passengerCount": 1,
    "passengers": [
        {
            "title": "MR",
            "firstName": "JOHN",
            "lastName": "SMITH",
            "dateOfBirth": "1990-01-01"
        }
    ],
    "contactDetails": {
        "email": "john@example.com",
        "phone": "44123456789"
    },
    "paymentDetails": {
        "cardType": "VISA",
        "cardNumber": "4444333322221111",
        "expiry": "0625",
        "cvv": "123"
    }
}
```

### Check-in

```http
POST /api/checkin/checkin
{
    "recordLocator": "ABC123",
    "passengerId": 1,
    "flightNumber": "VS001",
    "seatNumber": "12A"
}
```

## Setup

1. Clone the repository
2. Update connection strings in `local.settings.json`
3. Install dependencies: `dotnet restore`
4. Run the application: `dotnet run`

## Development

- Built with .NET 6.0
- Uses Azure Functions for API hosting
- In-memory repositories for development (can be replaced with actual databases)
- JWT authentication for API security

## Acknowledgments

- Inspired by traditional airline reservation systems
- Uses industry-standard practices for airline data management
