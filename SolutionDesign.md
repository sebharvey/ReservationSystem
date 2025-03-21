
## High Level Solution Design

```mermaid
graph TD
    %% Users
    Customer1[👤 Customer]
    Customer2[👤 Customer]
    Staff[👥 Airline Staff]
    OTA[👥 OTAs]
    Airlines[✈️ Other Airlines]

    %% Consumers Layer
    subgraph Consumers
        Web[Website]
        Mobile[Mobile App]
        Console[Console App]
        NDC[NDC]
    end

    %% API Layer
    subgraph Retail_APIs[Retail APIs]
        Offer[Res.Api.Offer]
        Order[Res.Api.Order]
        Delivery[Res.Api.Delivery]
    end
    subgraph Ops_APIs[Operations APIs]
        Command[Res.Api.Command]
        Interlining[Res.Api.Interlining]
    end

    %% Microservices Layer
    subgraph Microservices
        Inventory[Res.Microservices.Inventory]
        Reservation[Res.Microservices.Reservation]
        Fares[Res.Microservices.Fares]
        Customer[Res.Microservices.Customer]
        Payment[Res.Microservices.Payment]
        Seat[Res.Microservices.Seat]
    end

    %% Event Bus
    EventBus[Event Bus]

    %% Databases and External Services
    DB1[(Inventory DB)]
    DB2[(Reservation DB)]
    DB3[(Customer DB)]
    DB4[(Seat DB)]
    AI[AI Pricing Engine]
    
    %% New Components
    PaymentGateway[💳 Payment Gateway]
    RevenueAccounting[📊 Revenue Accounting System]

    %% User to Consumer connections
    Customer1 --> Web
    Customer2 --> Mobile
    Staff --> Console
    OTA --> NDC
    Airlines --> Interlining

    %% Consumer to API connections
    Web --> Offer
    Web --> Order
    Web --> Delivery
    Mobile --> Offer
    Mobile --> Order
    Mobile --> Delivery
    Console --> Command
    NDC --> Offer
    NDC --> Order

    %% API to Microservice connections
    Offer --> Inventory
    Offer --> Fares
    Offer --> Seat
    Order --> Inventory
    Order --> Reservation
    Order --> Fares
    Order --> Customer
    Order --> Payment
    Order --> Seat
    Delivery --> Inventory
    Delivery --> Reservation
    Delivery --> Seat
    Command --> Inventory
    Command --> Reservation
    Command --> Fares
    Command --> Seat
    Interlining --> Inventory
    Interlining --> Reservation
    Interlining --> Fares
    Interlining --> Seat

    %% Microservice to Database/External connections
    Inventory --> DB1
    Reservation --> DB2
    Customer --> DB3
    Seat --> DB4
    Fares --> AI
    
    %% Payment Gateway connection
    Payment --> PaymentGateway
    
    %% Event publishing
    Reservation --> EventBus
    
    %% Revenue Accounting connection
    EventBus --> RevenueAccounting

    %% Styling with black text
    classDef user fill:#e8f5e9,stroke:#2e7d32,color:black
    classDef consumer fill:#e1f5fe,stroke:#01579b,color:black
    classDef api fill:#fff3e0,stroke:#ff6f00,color:black
    classDef apiGroup fill:#f3e5f5,stroke:#4a148c,color:black
    classDef microservice fill:#fff3e0,stroke:#ff6f00,color:black
    classDef database fill:#fafafa,stroke:#212121,color:black
    classDef external fill:#f1f8e9,stroke:#33691e,color:black
    classDef eventbus fill:#ffebee,stroke:#b71c1c,color:black
    classDef payment fill:#e3f2fd,stroke:#0d47a1,color:black
    classDef revenue fill:#fff8e1,stroke:#ff8f00,color:black

    class Customer1,Customer2,Staff,OTA,Airlines user
    class Web,Mobile,Console,NDC consumer
    class Offer,Command,Order,Interlining,Delivery api
    class Retail_APIs,Ops_APIs,Microservices serviceGroup
    class Inventory,Reservation,Fares,Customer,Payment,Seat microservice
    class DB1,DB2,DB3,DB4 database
    class AI external
    class EventBus eventbus
    class PaymentGateway payment
    class RevenueAccounting revenue
```

## Flight booking

Search and Offer Phase:

Customer searches for flights via web or mobile app
Offer API checks flight availability through the Inventory microservice
Pricing options are retrieved from the Fares microservice using the AI Pricing Engine
Available flights with pricing are displayed to the customer

Order Initiation:

Customer selects flight, fare, passenger details, seat preferences, and enters payment information
Order API verifies flight availability, customer details, and pricing
Initial payment authorization is processed through Payment microservice and Payment Gateway

Reservation and Ticketing:

Upon successful authorization, Order API instructs Reservation microservice to create the PNR
Reservation microservice generates an e-ticket
Reservation microservice allocates the selected seats to the PNR through Inventory microservice

Ancillary Services (Paid Seats):

For paid seats, Reservation microservice requests additional payment processing
Order API coordinates authorization and capture with Payment microservice
Upon successful payment, Reservation microservice generates EMDs with references to the associated e-ticket

Event Publishing and Confirmation:

Reservation microservice publishes an 'OrderCreated' Cloud Event to the Event Bus
Event includes ticket and EMD details in the meta property
Event Bus forwards this to Revenue Accounting System as a one-way data feed (fire-and-forget)
Order API returns complete booking confirmation to the customer (PNR, e-ticket, EMDs)

This flow ensures proper sequencing of reservation creation, ticketing, seat allocation, and ancillary service documentation while maintaining the Order API as the central payment orchestrator.

```mermaid
sequenceDiagram
    actor Customer
    participant Web/Mobile as Web/Mobile App
    participant OfferAPI as Res.Api.Offer
    participant OrderAPI as Res.Api.Order
    participant InventoryMS as Res.Microservices.Inventory
    participant FaresMS as Res.Microservices.Fares
    participant ReservationMS as Res.Microservices.Reservation
    participant CustomerMS as Res.Microservices.Customer
    participant PaymentMS as Res.Microservices.Payment
    participant AIEngine as AI Pricing Engine
    participant PaymentGateway as Payment Gateway
    participant EventBus as Event Bus
    participant RevenueAccounting as Revenue Accounting System
    
    %% Search for flights
    Customer->>Web/Mobile: Search for flights (OND)
    Web/Mobile->>OfferAPI: Request available flights
    OfferAPI->>InventoryMS: Check flight availability
    InventoryMS-->>OfferAPI: Return available flights
    OfferAPI->>FaresMS: Request pricing for available flights
    FaresMS->>AIEngine: Get pricing based on availability, seasonality, market factors
    AIEngine-->>FaresMS: Return flex and non-flex fares per cabin
    FaresMS-->>OfferAPI: Return fare options
    OfferAPI-->>Web/Mobile: Return flight options with pricing
    Web/Mobile-->>Customer: Display flight options
    
    %% Select flight and create order
    Customer->>Web/Mobile: Select flight, fare, and enter passenger details
    Customer->>Web/Mobile: Select seats (including paid seats)
    Customer->>Web/Mobile: Enter payment details
    Web/Mobile->>OrderAPI: Create booking order
    
    %% Verify inventory and create reservation
    OrderAPI->>InventoryMS: Verify flight availability
    InventoryMS-->>OrderAPI: Confirm availability
    OrderAPI->>CustomerMS: Store/Verify customer details
    CustomerMS-->>OrderAPI: Return customer profile
    OrderAPI->>FaresMS: Verify pricing
    FaresMS-->>OrderAPI: Confirm pricing
    
    %% Payment process
    OrderAPI->>PaymentMS: Process payment
    PaymentMS->>PaymentGateway: Authorize payment
    PaymentGateway-->>PaymentMS: Payment authorized
    PaymentMS-->>OrderAPI: Payment authorization confirmed
    
    %% Create reservation
    OrderAPI->>ReservationMS: Create PNR
    ReservationMS->>ReservationMS: Generate e-ticket
    
    %% Handle seat allocation and EMD for paid seats after ticketing
    ReservationMS->>InventoryMS: Allocate seats to PNR
    InventoryMS-->>ReservationMS: Seats allocated
    
    %% Process payment for paid seats
    alt Paid seats selected
        ReservationMS->>OrderAPI: Request payment for paid seats
        OrderAPI->>PaymentMS: Authorize payment for seats
        PaymentMS->>PaymentGateway: Authorize seat payment
        PaymentGateway-->>PaymentMS: Seat payment authorized
        PaymentMS-->>OrderAPI: Seat payment authorization confirmed
        OrderAPI-->>ReservationMS: Seat payment authorized
        ReservationMS->>ReservationMS: Generate EMD for paid seats with e-ticket reference
        
        %% Capture payment for seats
        ReservationMS->>OrderAPI: Request payment capture for seats
        OrderAPI->>PaymentMS: Capture payment for seats
        PaymentMS->>PaymentGateway: Capture authorized seat payment
        PaymentGateway-->>PaymentMS: Seat payment captured
        PaymentMS-->>OrderAPI: Seat payment capture confirmed
        OrderAPI-->>ReservationMS: Seat payment captured
    end
    
    ReservationMS-->>OrderAPI: Return PNR, e-ticket, and EMD details
    
    %% Capture payment
    OrderAPI->>PaymentMS: Capture payment
    PaymentMS->>PaymentGateway: Capture authorized payment
    PaymentGateway-->>PaymentMS: Payment captured
    PaymentMS-->>OrderAPI: Payment capture confirmed
    
    %% Publish booking event
    ReservationMS->>EventBus: Publish 'OrderCreated' Cloud Event (with ticket/EMD in meta)
    EventBus->>RevenueAccounting: Forward 'OrderCreated' event
    
    %% Return booking confirmation
    OrderAPI-->>Web/Mobile: Return booking confirmation
    Web/Mobile-->>Customer: Display booking confirmation with PNR, e-ticket, and EMD (if applicable)
```




## Check In

```mermaid
sequenceDiagram
    actor Customer
    participant Web/Mobile as Web/Mobile App
    participant DeliveryAPI as Res.Api.Delivery
    participant ReservationMS as Res.Microservices.Reservation
    participant InventoryMS as Res.Microservices.Inventory
    
    %% Start check-in process
    Customer->>Web/Mobile: Launch check-in (input PNR record locator & departure airport)
    Web/Mobile->>DeliveryAPI: Retrieve booking details
    DeliveryAPI->>ReservationMS: Verify PNR eligibility for check-in
    ReservationMS->>ReservationMS: Validate e-ticket status
    ReservationMS->>ReservationMS: Verify within check-in window (24h)
    ReservationMS-->>DeliveryAPI: Return booking details & eligible passengers
    DeliveryAPI-->>Web/Mobile: Return flight & passenger details
    Web/Mobile-->>Customer: Display passenger selection screen
    
    %% Passenger selection
    Customer->>Web/Mobile: Select passengers to check in
    Web/Mobile->>DeliveryAPI: Submit selected passengers
    
    %% Passenger details verification/update
    Customer->>Web/Mobile: Update passenger information
    Web/Mobile->>DeliveryAPI: Update passenger details
    DeliveryAPI-->>Web/Mobile: Confirm details updated
    
    %% Hazardous substances acknowledgment
    Customer->>Web/Mobile: Acknowledge hazardous substances rules
    
    %% Complete online check-in
    Customer->>Web/Mobile: Complete check-in
    Web/Mobile->>DeliveryAPI: Finalize check-in
    
    Note over DeliveryAPI: Future integration: Security API check for passengers
    
    DeliveryAPI->>ReservationMS: Process check-in
    
    %% Seat assignment check and handling
    ReservationMS->>InventoryMS: Check seat assignments
    alt Seats not assigned
        InventoryMS->>InventoryMS: Assign random seats
    end
    InventoryMS-->>ReservationMS: Confirm seat assignments
    
    %% Generate boarding passes
    ReservationMS->>ReservationMS: Generate boarding passes
    ReservationMS-->>DeliveryAPI: Return boarding pass details
    DeliveryAPI-->>Web/Mobile: Return boarding passes
    Web/Mobile-->>Customer: Display boarding passes & delivery options
    
    %% Mobile wallet option
    alt Mobile App - Wallet
        Customer->>Web/Mobile: Request save to wallet
        Web/Mobile->>DeliveryAPI: Request boarding pass for wallet
        DeliveryAPI->>ReservationMS: Generate wallet format boarding pass
        ReservationMS-->>DeliveryAPI: Return wallet format boarding pass
        DeliveryAPI-->>Web/Mobile: Return wallet format boarding pass
        Web/Mobile-->>Customer: Add boarding pass to mobile wallet
    end
    
    %% PDF download option
    alt Website/Mobile - PDF Download
        Customer->>Web/Mobile: Request PDF download
        Web/Mobile->>DeliveryAPI: Request PDF boarding pass
        DeliveryAPI->>ReservationMS: Generate PDF boarding pass
        ReservationMS-->>DeliveryAPI: Return PDF boarding pass
        DeliveryAPI-->>Web/Mobile: Return PDF boarding pass
        Web/Mobile-->>Customer: Download boarding pass PDF
    end
    
    %% Email option
    alt Email Delivery
        Customer->>Web/Mobile: Request email boarding pass
        Web/Mobile->>DeliveryAPI: Request email boarding pass
        DeliveryAPI->>ReservationMS: Generate email format boarding pass
        ReservationMS-->>DeliveryAPI: Return email format boarding pass
        DeliveryAPI->>DeliveryAPI: Send email with boarding pass
        DeliveryAPI-->>Web/Mobile: Email sent confirmation
        Web/Mobile-->>Customer: Display email sent confirmation
    end
```
## IATA One Order Example Schema

```json
{
  "OrderID": "A1B2C3D4E5",
  "OrderStatus": "CONFIRMED",
  "OrderCreateDate": "2025-03-15T14:30:00Z",
  "Customer": {
    "CustomerID": "CUST123456",
    "GivenName": "Jane",
    "Surname": "Smith",
    "ContactInformation": {
      "EmailAddress": "jane.smith@example.com",
      "Phone": "+1-555-123-4567"
    },
    "LoyaltyProgram": {
      "ProgramName": "SkyMiles",
      "AccountNumber": "SM987654321",
      "Status": "Gold"
    }
  },
  "OrderItems": [
    {
      "OrderItemID": "OI-001",
      "ItemStatus": "CONFIRMED",
      "FlightItem": {
        "SegmentID": "SEG001",
        "Origin": "JFK",
        "Destination": "LHR",
        "DepartureDate": "2025-04-10T18:30:00Z",
        "ArrivalDate": "2025-04-11T07:15:00Z",
        "FlightNumber": "BA178",
        "CarrierCode": "BA",
        "ServiceClass": "J",
        "Seat": "12A",
        "Baggage": {
          "CheckedAllowance": "2PC",
          "CabinAllowance": "1PC"
        }
      }
    },
    {
      "OrderItemID": "OI-002",
      "ItemStatus": "CONFIRMED",
      "ServiceItem": {
        "ServiceID": "SRV001",
        "ServiceType": "MEAL",
        "ServiceDescription": "Special Meal - Vegetarian",
        "FlightReference": "SEG001"
      }
    },
    {
      "OrderItemID": "OI-003",
      "ItemStatus": "CONFIRMED",
      "ServiceItem": {
        "ServiceID": "SRV002",
        "ServiceType": "LOUNGE",
        "ServiceDescription": "Airport Lounge Access",
        "LocationCode": "JFK",
        "ValidityPeriod": {
          "StartDate": "2025-04-10T14:30:00Z",
          "EndDate": "2025-04-10T18:30:00Z"
        }
      }
    }
  ],
  "Payment": {
    "PaymentID": "PAY001",
    "PaymentMethod": "CreditCard",
    "PaymentStatus": "COMPLETED",
    "PaymentAmount": {
      "Amount": "1250.00",
      "CurrencyCode": "USD"
    },
    "PaymentDate": "2025-03-15T14:32:15Z"
  },
  "ServicingHistory": [
    {
      "EventID": "SRV-001",
      "EventType": "ORDER_CREATION",
      "EventTimestamp": "2025-03-15T14:30:00Z",
      "Agent": {
        "AgentID": "AGENT007",
        "AgentType": "SYSTEM"
      }
    },
    {
      "EventID": "SRV-002",
      "EventType": "SEAT_SELECTION",
      "EventTimestamp": "2025-03-15T14:35:22Z",
      "Agent": {
        "AgentID": "CUST123456",
        "AgentType": "CUSTOMER"
      }
    }
  ]
}
```

