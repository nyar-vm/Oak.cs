namespace Oak.Text;

/// <summary>
///     文本位置
/// </summary>
public readonly record struct TextPosition
{
    public TextPosition(int position, int line, int column)
    {
        Position = position;
        Line = line;
        Column = column;
    }

    /// <summary>
    ///     字符位置
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    ///     行号
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    ///     列号
    /// </summary>
    public int Column { get; init; }
}