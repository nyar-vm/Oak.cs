namespace Oak.Valkyrie.Query;

public sealed class QueryParseException : Exception
{
    public int Position { get; }

    public QueryParseException(int position, string message) : base($"位置 {position}: {message}")
    {
        Position = position;
    }
}