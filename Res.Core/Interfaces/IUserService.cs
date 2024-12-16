using Res.Domain.Responses;

namespace Res.Core.Interfaces
{
    public interface IUserService
    {
        AuthenticationResponse Authenticate(string userId, string password);
        TokenValidationResponse ValidateToken(string token);
        bool CreateUser(string userId, string password);
    }
}