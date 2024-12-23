using Res.Core.Interfaces;
using Res.Api.Models;
using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;
using CheckInRequest = Res.Api.Models.CheckInRequest;

namespace Res.Api.Core.Services
{
    public class CheckinService : ICheckinService
    {
        private readonly ICheckInService _checkinService;
        private readonly IReservationService _reservationService;
        private readonly IApisService _apisService;

        public CheckinService(ICheckInService checkinService, IReservationService reservationService, IApisService apisService)
        {
            _checkinService = checkinService;
            _reservationService = reservationService;
            _apisService = apisService;
        }

        public async Task<ValidateCheckInResponse?> ValidateCheckin(ValidateCheckInRequest request)
        {
            try
            {
                var pnr = await _checkinService.ValidatePnr(request.RecordLocator, request.From);

                // Find matching flight segment
                var segment = pnr.Data.Segments.FirstOrDefault(s => s.Origin == request.From);

                // Map passengers with their tickets
                var passengers = pnr.Data.Passengers.Select(p => new ValidateCheckInResponse.PassengerInfo
                {
                    PassengerId = p.PassengerId,
                    Title = p.Title,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Type = p.Type.ToString(),
                    Ticket = pnr.Data.Tickets.FirstOrDefault(t => t.PassengerId == p.PassengerId)?.TicketNumber
                }).ToList();

                // Return success with PNR details
                return new ValidateCheckInResponse
                {
                    Success = true,
                    RecordLocator = pnr.RecordLocator,
                    FlightNumber = segment.FlightNumber,
                    Origin = segment.Origin,
                    Destination = segment.Destination,
                    DepartureDate = segment.DepartureDate,
                    DepartureTime = segment.DepartureTime,
                    Passengers = passengers
                };
            }
            catch (Exception ex)
            {
                return new ValidateCheckInResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<CheckInResponse?> CheckIn(CheckInRequest checkInRequest)
        {
            try
            {
                // 1. Retrieve PNR and perform initial validations
                await _reservationService.RetrievePnr(checkInRequest.RecordLocator);
                
                if (_reservationService.Pnr == null)
                    throw new Exception("PNR NOT FOUND");

                // 2. Find matching flight segment
                var segment = _reservationService.Pnr.Data.Segments.FirstOrDefault(s => s.FlightNumber == checkInRequest.From);
                if (segment == null)
                    throw new Exception("SEGMENT NOT FOUND");

                // 3. Create passport information SSRs for each passenger
                foreach (var passenger in _reservationService.Pnr.Data.Passengers)
                {
                    // Find matching passenger info from request
                    var passengerInfo = checkInRequest.PassengerInfo?
                        .FirstOrDefault(p => p.PassengerId == passenger.PassengerId);

                    if (passengerInfo == null)
                        throw new Exception($"Missing passenger information for passenger {passenger.PassengerId}");

                    var docsSsr = new Ssr
                    {
                        Code = "DOCS",
                        PassengerId = passenger.PassengerId,
                        Status = SsrStatus.Confirmed,
                        ActionCode = "HK",
                        CompanyId = segment.FlightNumber.Substring(0, 2),
                        Quantity = 1,
                        Text = $"HK1/{passengerInfo.DocType}/" +
                               $"{passengerInfo.IssuingCountry}/" +
                               $"{passengerInfo.DocNumber}/" +
                               $"{passengerInfo.Nationality}/" +
                               $"{passengerInfo.Dob.ToString("ddMMMyyyy").ToUpper()}/" +
                               $"{passengerInfo.Gender}/" +
                               $"{passengerInfo.ExpiryDate.ToString("ddMMMyyyy").ToUpper()}/" +
                               $"{passenger.FirstName}/" +
                               $"{passenger.LastName}"
                    };

                    _reservationService.Pnr.Data.SpecialServiceRequests.Add(docsSsr);

                    // Add APIS address SSR for each passenger
                    if (checkInRequest.ApisInformation != null)
                    {
                        var apisAddressSsr = new Ssr
                        {
                            Code = "DOCA",
                            PassengerId = passenger.PassengerId,
                            Status = SsrStatus.Confirmed,
                            ActionCode = "HK",
                            CompanyId = segment.FlightNumber.Substring(0, 2),
                            Quantity = 1,
                            Text = $"HK1/{checkInRequest.ApisInformation.Type}/" +
                                   $"{checkInRequest.ApisInformation.Country}/" +
                                   $"{checkInRequest.ApisInformation.Street}/" +
                                   $"{checkInRequest.ApisInformation.City}/" +
                                   $"{checkInRequest.ApisInformation.State}/" +
                                   $"{checkInRequest.ApisInformation.Postal}"
                        };

                        _reservationService.Pnr.Data.SpecialServiceRequests.Add(apisAddressSsr);
                    }
                }

                // 4. Build APIS data for all passengers
                var apisData = new ApisData
                {
                    RecordLocator = checkInRequest.RecordLocator,
                    FlightNumber = checkInRequest.From,
                    DepartureDate = segment.DepartureDate,
                    Passengers = _reservationService.Pnr.Data.Passengers.Select(passenger =>
                    {
                        var passengerInfo = checkInRequest.PassengerInfo
                            .First(p => p.PassengerId == passenger.PassengerId);

                        return new PassengerApis
                        {
                            PassengerId = passenger.PassengerId,
                            FirstName = passenger.FirstName,
                            LastName = passenger.LastName,
                            DocumentType = passengerInfo.DocType,
                            DocumentNumber = passengerInfo.DocNumber,
                            DocumentIssuingCountry = passengerInfo.IssuingCountry,
                            DocumentExpiryDate = passengerInfo.ExpiryDate,
                            DateOfBirth = passengerInfo.Dob,
                            Gender = passengerInfo.Gender,
                            Nationality = passengerInfo.Nationality,

                            // Same APIS address info for all passengers as per requirement
                            CountryOfResidence = checkInRequest.ApisInformation.Country,
                            ResidenceAddress = checkInRequest.ApisInformation.Street,
                            ResidenceCity = checkInRequest.ApisInformation.City,
                            ResidenceState = checkInRequest.ApisInformation.State,
                            ResidencePostalCode = checkInRequest.ApisInformation.Postal
                        };
                    }).ToList()
                };

                // 5. Submit APIS data
                if (!await _apisService.ValidateApisData(apisData))
                    throw new Exception("INVALID APIS DATA");

                if (!await _apisService.SubmitApisData(apisData))
                    throw new Exception("APIS SUBMISSION FAILED");

                // 6. Check in all passengers
                List<BoardingPass> boardingPasses = await _checkinService.CheckInAll(checkInRequest.RecordLocator, segment.FlightNumber);

                // 7. Save all changes to PNR
                await _reservationService.CommitPnr();

                return new CheckInResponse
                {
                    Success = true,
                    Message = $"Successfully checked in {boardingPasses.Count} passengers",
                    BoardingPasses = boardingPasses // Include boarding passes in response
                };
            }
            catch (Exception ex)
            {
                return new CheckInResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }
}
