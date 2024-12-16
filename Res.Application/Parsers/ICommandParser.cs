namespace Res.Application.Parsers
{
    public interface ICommandParser<T> where T : class
    {
        T Parse(string command);
    }
}
