namespace Res.Domain.Dto
{
    public class CommandResult
    {
        public bool Success { get; set; }
        public object Response { get; set; }
        public string Message { get; set; }
        public Guid? SessionId { get; set; }
    }
}