namespace Oak.GraphQL;

public readonly struct GqlToken
{
    public GqlTokenType Type { get; }
    public string Text { get; }
    public int Line { get; }
    public int Column { get; }

    public GqlToken(GqlTokenType type, string text, int line, int column)
    {
        Type = type;
        Text = text;
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return $"{Type}('{Text}') @ {Line}:{Column}";
    }
}
