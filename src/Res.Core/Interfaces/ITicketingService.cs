using Res.Domain.Entities.Pnr;
using Res.Domain.Entities.Ticket;

namespace Res.Core.Interfaces
{
    public interface ITicketingService
    {
        Task<List<Ticket>> IssueTickets(Pnr pnr);
        Task<bool> VoidTicket(string ticketNumber);
        Task<bool> RefundTicket(string ticketNumber);
        string GenerateTicketNumber(string airlineCode);
    }
}