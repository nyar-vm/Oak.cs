namespace Oak.Parsing;

/// <summary>
///     解析异常
/// </summary>
public sealed class ParseException : Exception
{
    public ParseException(string message) : base(message)
    {
    }

    public ParseException(string message, Exception inner) : base(message, inner)
    {
    }
}