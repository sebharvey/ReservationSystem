namespace Res.Domain.Enums
{
    public enum CommandType
    {
        Availability,    // AN - Availability search
        SellSegment,           // SS - Sell segment
        Retrieve,       // RT - Retrieve PNR
        DisplayPnr,              // RT<recordLocator> - existing
        DisplayAllPnr,          // RTALL - existing
        RetrieveByName,         // RTNSMITH/JOHN
        RetrieveByFlight,       // RTVS001/24NOV
        RetrieveByPhone,        // RTCT442071234567
        RetrieveByTicket,       // RTTK9321234567890
        RetrieveByFreqFlyer,    // RTFF12345678
        EndTransaction,         // ER - End transaction and recall
        EndTransactionClear,   // ET - End transaction and clear
        AddName,        // NM - Add passenger name
        Cancel,         // XE - Cancel element
        Issue,          // TKT - Issue ticket
        AddSsr,         // SR - Add special service request
        AddOsi,         // OS - Add other service info
        Display,        // *R - Display PNR
        ChangeClass,    // RC - Rebook in different class
        Queue,          // QP - Queue place
        Ignore,         // IG - Ignore transaction
        AddTicketingArrangement,
        AddContact,
        AddAgency,    // RF - Received From
        Help,
        Remark,
        RemoveSegment,
        PricePnr,        // FXP - Price PNR
        StoreFare,       // FS - Store fare
        FormOfPayment,   // FP - Form of payment
        DisplayFareQuote,
        DisplayFareNotes,
        DisplayFareHistory,
        DisplayFareRules,
        IssueTicket,
        TicketDisplay,
        Json,
        DeleteSsr,       // SRXK1      
        ListSsr,         // SR*
        CheckIn,
        CancelCheckIn,
        PrintBoardingPass,
        AddDocument,
        DeleteDocument,
        ListDocuments,
        CheckInAll,
        TicketListDisplay,
        RetrieveByFrequentFlyer,
        DisplayApis,
        AddApiAddress,
        DisplaySeatMap,
        ArrivalUnknown,
        AssignSeat,
        RemoveSeat,
        DeletePnr,        // XI - Delete PNR
    }
}