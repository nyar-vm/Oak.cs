namespace Oak.Glsl;

public readonly struct GlslToken
{
    public GlslTokenType Type { get; }
    public string Text { get; }
    public int Line { get; }
    public int Column { get; }

    public GlslToken(GlslTokenType type, string text, int line, int column)
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
