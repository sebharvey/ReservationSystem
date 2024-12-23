namespace Res.Domain.Responses
{
    public class UserContext
    {
        public string UserId { get; set; }
        public string Role { get; set; }
        public string AgentId { get; set; }
        public DateTime TokenExpiry { get; set; }
        public Guid SessionId { get; set; }
    }
}