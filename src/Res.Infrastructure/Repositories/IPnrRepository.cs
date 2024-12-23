using Res.Domain.Entities.Pnr;

namespace Res.Infrastructure.Repositories
{
    public interface IPnrRepository
    {
        Task<Pnr> GetByRecordLocator(string recordLocator);
        Task<List<Pnr>> GetByLastName(string lastName, string firstName = null);
        Task<bool> Save(Pnr pnr, bool commit = false);
        Task<bool> Delete(string recordLocator);
        Task<List<Pnr>> GetAll();
        Task<Pnr?> GetBySessionId(Guid userContextSessionId);
    }
}