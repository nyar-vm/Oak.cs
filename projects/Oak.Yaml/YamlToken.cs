namespace Oak.Yaml;

/// <summary>
///     YAML 词法单元
/// </summary>
public readonly struct YamlToken
{
    /// <summary>
    ///     词法单元类型
    /// </summary>
    public YamlTokenType Type { get; }

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

    /// <summary>
    ///     缩进级别
    /// </summary>
    public int Indent { get; }

    public YamlToken(YamlTokenType type, string text, int line, int column, int indent = 0)
    {
        Type = type;
        Text = text;
        Line = line;
        Column = column;
        Indent = indent;
    }

    public override string ToString()
    {
        return $"{Type}('{Text}') @ {Line}:{Column} indent={Indent}";
    }
}