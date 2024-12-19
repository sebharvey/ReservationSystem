using Res.Domain.Entities.User;

namespace Res.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        public List<User> Users { get; set; }
    }
}