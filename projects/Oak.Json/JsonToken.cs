namespace Oak.Json;

/// <summary>
///     JSON 词法单元
/// </summary>
public readonly struct JsonToken
{
    /// <summary>
    ///     词法单元类型
    /// </summary>
    public JsonTokenType Type { get; }

    /// <summary>
    ///     原始文本
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     行号（从 1 开始）
    /// </summary>
    public int Line { get; }

    /// <summary>
    ///     列号（从 1 开始）
    /// </summary>
    public int Column { get; }

    public JsonToken(JsonTokenType type, string text, int line, int column)
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