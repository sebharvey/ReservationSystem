namespace Res.Domain.Responses
{
    public class TokenValidationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserContext UserContext { get; set; }
    }
}