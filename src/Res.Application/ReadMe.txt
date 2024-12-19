# Airline Reservation System Command Guide

This guide covers the essential commands for creating and managing Passenger Name Records (PNRs) in the reservation system.

## Basic PNR Creation

### Flight Search and Booking
- `AN24NOVLHRJFK/1400` - Search flights (date/origin/destination/optional time)
- `SS1Y2` - Sell seat from availability display (1=quantity, Y=class, 2=line number)
- `SS VS001Y24NOVLHRJFK1` - Direct sell (flight/class/date/origin/dest/quantity)
- `XE1` - Remove segment number 1 from PNR

### Passenger Details
- `NM1SMITH/JOHN MR` - Add passenger (surname/firstname title)
- `CTCP 44123456789` - Add phone contact
- `CTCE JOHN@EMAIL.COM` - Add email contact
- `AGY ABC/12345678/JAGENT` - Add agency details (code/iata/agent)
- `TLTL24NOV/VS` - Add ticketing time limit and carrier
- `RM FREQUENT FLYER 123` - Add a general remark

### PNR Management
- `ER` - End transaction and save PNR
- `IG` - Ignore/cancel current transaction
- `*R` - Display current PNR

## PNR Retrieval Commands

### Basic Retrieval
- `RTABCDEF` - Retrieve PNR by record locator
- `RTALL` - List all PNRs

### Advanced Retrieval Options
- `RTNSMITH/JOHN` - Retrieve by passenger name (surname/firstname)
- `RTVS001/24NOV` - Retrieve by flight number and date
- `RTCT442071234567` - Retrieve by phone number
- `RTTK9321234567890` - Retrieve by ticket number
- `RTFF12345678` - Retrieve by frequent flyer number

## Special Service Requests (SSR)

### Managing SSRs
- `SR WCHR/P1/S1/TXT` - Add SSR (code/passenger/segment/text)
- `SRXK1` - Delete SSR by ID
- `SR*` - List all SSRs

Common SSR codes:
- WCHR: Wheelchair
- VGML: Vegetarian meal
- DBML: Diabetic meal
- SPML: Special meal
- UMNR: Unaccompanied minor
- PETC: Pet in cabin
- DOCS: Travel document info

## Travel Documents

### Document Management
- `SRDOCS HK1/P/GBR/P12345678/GBR/12JUL82/M/20NOV25/SMITH/JOHN` - Add passport
  - Format: Type/Country/Number/Nationality/DOB/Gender/Expiry/Name
- `SRXD1` - Delete document 1
- `DOCS*` - List all documents

## Pricing and Ticketing

### Fare Commands
- `FXP` - Price PNR at published fares
	--	FXP/R,FC-USD
	--	FXP/R,FC-EUR
	--	FXP/R,FC-GBP
- `FP*CC/VISA/4444333322221111/0625/GBP892.00` - Add form of payment (credit card)
- `FP*CA/GBP892.00`							   - Add form of payment (cash)
- `FP*MS/INVOICE/GBP892.00`					   - Add form of payment (misc)
- `FS` - Store the fare
- `FQD` - Display fare quote
- `FN` - Display fare notes/rules
- `FH` - Display fare history
- `FV*` - Display detailed fare rules

### Ticketing Commands
- `TTP` - Issue tickets for all passengers
- `TKTL` - List all tickets in PNR
- `TKT/NUMBER` - Display specific ticket details

## Check-in Services

### Check-in Commands
- `CKIN ABC123/P1/VS001/12A` - Check in (PNR/Pax/Flight/Seat)
- `CKIN ABC123/P1/VS001/12A/B2/20.5` - Check in with bags (Count/Weight)
- `CXLD ABC123/P1/VS001` - Cancel check-in
- `CKINALL ABC123/VS001` - Check in all passengers
- `PRNT ABC123/P1/VS001` - Print boarding pass

## Example Workflow

1. Search for flights:
```
AN24NOVLHRJFK/1400
```

2. Create PNR with passengers and flights:
```
NM1SMITH/JOHN MR
NM2SMITH/JANE MRS
SS1Y2
CTCP 44123456789
CTCE JOHN@EMAIL.COM
TLTL24NOV/VS
ER
```

3. Add documents and SSRs:
```
SRDOCS HK1/P/GBR/P12345678/GBR/12JUL82/M/20NOV25/SMITH/JOHN
SR VGML/P1/S1
ER
```

4. Price and ticket:
```
FXP
FS
FP*CC/VISA/4444333322221111/0625/GBP892.00		-- credit card
FP*CA/GBP892.00									-- cash
FP*MS/INVOICE/GBP892.00							-- misc
TTP
```

5. Check-in:
```
CKIN ABC123/P1/VS001/12A
```

## Additional Notes

- Always end transactions with `ER` to save changes
- Use `*R` to verify PNR contents after changes
- Commands are case-insensitive
- Use `HELP` to display all available commands
- Use `*J` to display PNR in JSON format

## Command Help

Type `HELP` in the system to display a full list of available commands and their formats.