-- Create schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'res')
BEGIN
    EXEC('CREATE SCHEMA res')
END
GO

-- Drop tables if they exist
IF OBJECT_ID('res.Pnr', 'U') IS NOT NULL DROP TABLE res.Pnr;
IF OBJECT_ID('res.FlightInventory', 'U') IS NOT NULL DROP TABLE res.FlightInventory;
IF OBJECT_ID('res.FlightSeatInventory', 'U') IS NOT NULL DROP TABLE res.FlightSeatInventory;
GO

-- Create PNR Table
CREATE TABLE res.Pnr (
    RecordLocator VARCHAR(6) PRIMARY KEY,
    Data NVARCHAR(MAX) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Create Flight Inventory Table
CREATE TABLE res.FlightInventory (
    FlightNo NVARCHAR(10) NOT NULL,
    Origin NVARCHAR(3) NOT NULL,
    Destination NVARCHAR(3) NOT NULL,
    DepartureDate NVARCHAR(10) NOT NULL,  -- Format: ddMMMyy
    ArrivalDate NVARCHAR(10) NOT NULL,    -- Format: ddMMMyy
    Seats NVARCHAR(MAX) NOT NULL,         -- JSON: Dictionary<string, int>
    DepartureTime NVARCHAR(4) NOT NULL,   -- Format: HHmm
    ArrivalTime NVARCHAR(4) NOT NULL,     -- Format: HHmm
    AircraftType NVARCHAR(10) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_FlightInventory PRIMARY KEY (FlightNo, DepartureDate)
);
GO

-- Create Flight Seat Inventory Table
CREATE TABLE res.FlightSeatInventory (
    FlightNumber NVARCHAR(10) NOT NULL,
    DepartureDate NVARCHAR(10) NOT NULL,  -- Format: ddMMMyy
    OccupiedSeats NVARCHAR(MAX) NOT NULL, -- JSON: HashSet<string>
    AircraftType NVARCHAR(10) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_FlightSeatInventory PRIMARY KEY (FlightNumber, DepartureDate)
);
GO

-- Create indexes for PNR JSON queries
CREATE INDEX IX_Pnr_Passengers ON res.Pnr
(RecordLocator)
INCLUDE (Data)
WHERE Data IS NOT NULL;

CREATE INDEX IX_Pnr_DateCreated ON res.Pnr
(CreatedDate);

-- Create indexes for Flight Inventory
CREATE INDEX IX_FlightInventory_Route ON res.FlightInventory
(Origin, Destination, DepartureDate);

CREATE INDEX IX_FlightInventory_DepartureDate ON res.FlightInventory
(DepartureDate)
INCLUDE (FlightNo, Origin, Destination, Seats, DepartureTime, ArrivalTime);

-- Create indexes for Flight Seat Inventory
CREATE INDEX IX_FlightSeatInventory_Flight ON res.FlightSeatInventory
(FlightNumber, DepartureDate)
INCLUDE (OccupiedSeats);

-- Create triggers for LastModifiedDate
CREATE TRIGGER res.trg_Pnr_UpdateModifiedDate
ON res.Pnr
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE res.Pnr
    SET LastModifiedDate = GETUTCDATE()
    FROM res.Pnr p
    INNER JOIN inserted i ON p.RecordLocator = i.RecordLocator;
END;
GO

CREATE TRIGGER res.trg_FlightInventory_UpdateModifiedDate
ON res.FlightInventory
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE res.FlightInventory
    SET LastModifiedDate = GETUTCDATE()
    FROM res.FlightInventory f
    INNER JOIN inserted i ON f.FlightNo = i.FlightNo AND f.DepartureDate = i.DepartureDate;
END;
GO

CREATE TRIGGER res.trg_FlightSeatInventory_UpdateModifiedDate
ON res.FlightSeatInventory
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE res.FlightSeatInventory
    SET LastModifiedDate = GETUTCDATE()
    FROM res.FlightSeatInventory f
    INNER JOIN inserted i ON f.FlightNumber = i.FlightNumber AND f.DepartureDate = i.DepartureDate;
END;
GO

-- Create stored procedure for validating JSON data
CREATE OR ALTER PROCEDURE res.ValidateFlightInventoryJson
    @Seats NVARCHAR(MAX)
AS
BEGIN
    IF ISJSON(@Seats) = 0
        THROW 50000, 'Invalid JSON format for Seats', 1;
        
    IF NOT EXISTS (
        SELECT 1
        FROM OPENJSON(@Seats)
        WHERE type = 'Number'
    )
        THROW 50000, 'Seats must contain numeric values', 1;
END;
GO

CREATE OR ALTER PROCEDURE res.ValidateFlightSeatInventoryJson
    @OccupiedSeats NVARCHAR(MAX)
AS
BEGIN
    IF ISJSON(@OccupiedSeats) = 0
        THROW 50000, 'Invalid JSON format for OccupiedSeats', 1;
        
    IF NOT EXISTS (
        SELECT 1
        FROM OPENJSON(@OccupiedSeats)
        WHERE type = 'String'
    )
        THROW 50000, 'OccupiedSeats must contain string values', 1;
END;
GO

-- Add check constraints for data validation
ALTER TABLE res.FlightInventory
ADD CONSTRAINT CHK_FlightInventory_Origin 
CHECK (LEN(Origin) = 3);

ALTER TABLE res.FlightInventory
ADD CONSTRAINT CHK_FlightInventory_Destination 
CHECK (LEN(Destination) = 3);

ALTER TABLE res.FlightInventory
ADD CONSTRAINT CHK_FlightInventory_Times 
CHECK (
    LEN(DepartureTime) = 4 AND 
    LEN(ArrivalTime) = 4 AND
    ISNUMERIC(DepartureTime) = 1 AND 
    ISNUMERIC(ArrivalTime) = 1
);

-- Create view for flight availability
CREATE OR ALTER VIEW res.vw_FlightAvailability
AS
SELECT 
    f.FlightNo,
    f.Origin,
    f.Destination,
    f.DepartureDate,
    f.DepartureTime,
    f.ArrivalDate,
    f.ArrivalTime,
    f.AircraftType,
    f.Seats,
    si.OccupiedSeats
FROM res.FlightInventory f
LEFT JOIN res.FlightSeatInventory si 
    ON f.FlightNo = si.FlightNumber 
    AND f.DepartureDate = si.DepartureDate;
GO

-- Grant permissions (adjust as needed)
GRANT SELECT, INSERT, UPDATE, DELETE ON res.Pnr TO [YourApplicationRole];
GRANT SELECT, INSERT, UPDATE, DELETE ON res.FlightInventory TO [YourApplicationRole];
GRANT SELECT, INSERT, UPDATE, DELETE ON res.FlightSeatInventory TO [YourApplicationRole];
GRANT EXECUTE ON res.ValidateFlightInventoryJson TO [YourApplicationRole];
GRANT EXECUTE ON res.ValidateFlightSeatInventoryJson TO [YourApplicationRole];
GO



-- Create SeatConfiguration Table
CREATE TABLE res.SeatConfiguration (
    AircraftType NVARCHAR(10) NOT NULL PRIMARY KEY,
    Cabins NVARCHAR(MAX) NOT NULL,  -- JSON
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create index for JSON queries
CREATE INDEX IX_SeatConfiguration_Cabins
ON res.SeatConfiguration (AircraftType)
INCLUDE (Cabins);

-- Create trigger for LastModifiedDate
CREATE TRIGGER res.trg_SeatConfiguration_UpdateModifiedDate
ON res.SeatConfiguration
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE res.SeatConfiguration
    SET LastModifiedDate = GETUTCDATE()
    FROM res.SeatConfiguration s
    INNER JOIN inserted i ON s.AircraftType = i.AircraftType;
END;

-- Create stored procedure for validating cabin configuration JSON
CREATE OR ALTER PROCEDURE res.ValidateSeatConfigurationJson
    @Cabins NVARCHAR(MAX)
AS
BEGIN
    IF ISJSON(@Cabins) = 0
        THROW 50000, 'Invalid JSON format for Cabins', 1;
    
    -- Validate cabin structure
    IF NOT EXISTS (
        SELECT 1
        FROM OPENJSON(@Cabins)
        WHERE type = 'object'
    )
        THROW 50000, 'Cabins must contain valid cabin configurations', 1;
END;