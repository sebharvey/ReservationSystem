namespace Res.Domain.Requests
{
    public class SsrRequest
    {
        public string Code { get; set; }
        public int PassengerId { get; set; }
        public int SegmentNumber { get; set; }
        public string Text { get; set; }
    }
}