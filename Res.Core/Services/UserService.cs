using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Res.Core.Interfaces;
using Res.Domain.Responses;
using Res.Infrastructure.Interfaces;

namespace Res.Core.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly string _secretKey = "YourSecretKeyHere12345678901234567890"; // Must be at least 32 characters
        private readonly string _algorithm = SecurityAlgorithms.HmacSha256;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public AuthenticationResponse Authenticate(string userId, string password)
        {
            var user = _userRepository.Users.FirstOrDefault(u => u.UserId == userId && u.Password == password);

            if (user == null)
            {
                _logger.LogInformation($"LOGIN FAILED FOR USER {userId}`");
                return new AuthenticationResponse { Success = false, Message = "Invalid credentials" };
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, _algorithm);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userId),
                new Claim(ClaimTypes.Role, "Agent"),
                new Claim("AgentId", userId)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            _logger.LogInformation($"LOGIN SUCCESSFUL FOR USER {userId}");

            return new AuthenticationResponse
            {
                Success = true,
                Token = tokenHandler.WriteToken(token)
            };
        }

        public TokenValidationResponse ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, parameters, out SecurityToken validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;

                return new TokenValidationResponse
                {
                    Success = true,
                    UserContext = new UserContext
                    {
                        UserId = principal.Identity.Name,
                        Role = principal.FindFirst(ClaimTypes.Role)?.Value,
                        AgentId = principal.FindFirst("AgentId")?.Value,
                        TokenExpiry = jwtToken.ValidTo
                    }
                };
            }
            catch (SecurityTokenExpiredException)
            {
                return new TokenValidationResponse
                {
                    Success = false,
                    Message = "Token has expired"
                };
            }
            catch (Exception ex)
            {
                return new TokenValidationResponse
                {
                    Success = false,
                    Message = $"Invalid token: {ex.Message}"
                };
            }
        }

        public bool CreateUser(string userId, string password)
        {
            throw new NotImplementedException();
        }
    }
}