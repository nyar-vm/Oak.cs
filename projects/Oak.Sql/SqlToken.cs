namespace Oak.Sql;

public readonly struct SqlToken
{
    public SqlTokenType Type { get; }
    public string Text { get; }
    public int Line { get; }
    public int Column { get; }

    public SqlToken(SqlTokenType type, string text, int line, int column)
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
