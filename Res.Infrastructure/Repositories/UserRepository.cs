using Res.Domain.Entities.User;
using Res.Infrastructure.Interfaces;

namespace Res.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private static List<User> _user = new();

        public List<User> Users
        {
            get => _user;
            set => _user = value;
        }
    }
}