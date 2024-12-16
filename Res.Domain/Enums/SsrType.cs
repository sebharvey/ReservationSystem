namespace Res.Domain.Enums
{
    public enum SsrType
    {
        // Passenger Assistance
        Wheelchair,           // WCHR, WCHS, WCHC
        MedicalAssistance,   // MEDA
        UnaccompaniedMinor,  // UMNR
        MeetAndAssist,       // MAAS

        // Dietary
        MealPreference,      // VGML, HNML, AVML, etc.
        SpecialMeal,         // SPML

        // Equipment/Animals
        ServiceAnimal,       // SVAN
        EmotionalSupport,    // ESAN
        SportingEquipment,   // SPEQ

        // Seating
        ExtraSeat,          // EXST
        BassinetSeat,       // BSCT

        // Baggage
        ExtraBaggage,       // XBAG
        FragileItems,       // FRAG

        // Other Services
        GroundService,      // GRPS
        FrequentFlyer,      // FQTV
        GroupHandling,      // GRPH
        CorporateHandling,   // CORP

        // Document related
        Passport,     // DOCS for passport details
        Visa,        // DOCO for visa details
        Address,     // DOCA for address details
        Identity,    // DOCS for other ID documents
    }
}