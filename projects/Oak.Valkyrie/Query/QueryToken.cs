namespace Oak.Valkyrie.Query;

public sealed class QueryToken
{
    public QueryTokenType Type { get; }
    public string Value { get; }
    public int Position { get; }

    public QueryToken(QueryTokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public override string ToString() => $"{Type}({Value})";
}
