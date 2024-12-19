namespace Res.Application.Parsers.Factory
{
    public interface ICommandParserFactory
    {
        ICommandParser<T> GetParser<T>() where T : class;
    }
}
