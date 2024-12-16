using Res.Domain.Entities.Pnr;
using Res.Infrastructure.Interfaces;

namespace Res.Infrastructure.Repositories
{
    public class PnrRepository : IPnrRepository
    {
        private static List<Pnr> _pnrs = new();

        public List<Pnr> Pnrs
        {
            get => _pnrs;
            set => _pnrs = value;
        }
    }
}