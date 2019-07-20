using System.Text;

namespace Oak.Text;

/// <summary>
///     文本读取器，提供通用的基于位置的文本扫描功能
/// </summary>
public sealed class TextReader
{
    public TextReader(string source)
    {
        Source = source;
        Position = 0;
        Line = 1;
        Column = 1;
    }

    /// <summary>
    ///     源代码内容
    /// </summary>
    public string Source { get; }

    /// <summary>
    ///     当前位置
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    ///     当前行号（从 1 开始）
    /// </summary>
    public int Line { get; private set; }

    /// <summary>
    ///     当前列号（从 1 开始）
    /// </summary>
    public int Column { get; private set; }

    /// <summary>
    ///     是否已到达末尾
    /// </summary>
    public bool IsAtEnd => Position >= Source.Length;

    /// <summary>
    ///     剩余字符数
    /// </summary>
    public int Remaining => Source.Length - Position;

    /// <summary>
    ///     查看当前字符
    /// </summary>
    public char Peek()
    {
        return IsAtEnd ? '\0' : Source[Position];
    }

    /// <summary>
    ///     查看指定偏移处的字符
    /// </summary>
    public char Peek(int offset)
    {
        var index = Position + offset;
        return index >= Source.Length ? '\0' : Source[index];
    }

    /// <summary>
    ///     前进一个字符并返回
    /// </summary>
    public char Advance()
    {
        if (IsAtEnd) return '\0';

        var c = Source[Position];
        Position++;

        if (c == '\n')
        {
            Line++;
            Column = 1;
        }
        else
        {
            Column++;
        }

        return c;
    }

    /// <summary>
    ///     尝试匹配指定字符
    /// </summary>
    public bool Match(char expected)
    {
        if (Peek() != expected) return false;

        Advance();
        return true;
    }

    /// <summary>
    ///     尝试匹配指定字符串
    /// </summary>
    public bool Match(string expected)
    {
        if (Position + expected.Length > Source.Length) return false;

        if (Source.Substring(Position, expected.Length) != expected) return false;

        for (var i = 0; i < expected.Length; i++) Advance();

        return true;
    }

    /// <summary>
    ///     跳过空白字符
    /// </summary>
    public void SkipWhitespace()
    {
        while (!IsAtEnd && char.IsWhiteSpace(Peek())) Advance();
    }

    /// <summary>
    ///     跳过空白字符和注释（行注释以 # 开头，块注释为 &lt;# #&gt;）
    /// </summary>
    public void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd)
            if (char.IsWhiteSpace(Peek()))
                SkipWhitespace();
            else if (Peek() == '#' && Peek(1) == '?')
                break;
            else if (Peek() == '#')
                SkipLineComment();
            else if (Peek() == '<' && Peek(1) == '#')
                SkipBlockComment();
            else
                break;
    }

    /// <summary>
    ///     跳过行注释
    /// </summary>
    public void SkipLineComment()
    {
        while (!IsAtEnd && Peek() != '\n') Advance();
    }

    /// <summary>
    ///     跳过块注释
    /// </summary>
    public void SkipBlockComment()
    {
        Advance();
        Advance();

        var depth = 1;

        while (!IsAtEnd && depth > 0)
            if (Peek() == '<' && Peek(1) == '#')
            {
                Advance();
                Advance();
                depth++;
            }
            else if (Peek() == '#' && Peek(1) == '>')
            {
                Advance();
                Advance();
                depth--;
            }
            else
            {
                Advance();
            }
    }

    /// <summary>
    ///     读取标识符
    /// </summary>
    public string ReadIdentifier()
    {
        var start = Position;

        while (!IsAtEnd && IsIdentifierPart(Peek())) Advance();

        return Source[start..Position];
    }

    /// <summary>
    ///     读取字符串直到指定字符
    /// </summary>
    public string ReadUntil(char terminator)
    {
        var start = Position;

        while (!IsAtEnd && Peek() != terminator) Advance();

        return Source[start..Position];
    }

    /// <summary>
    ///     读取字符串直到指定条件成立
    /// </summary>
    public string ReadWhile(Func<char, bool> predicate)
    {
        var start = Position;

        while (!IsAtEnd && predicate(Peek())) Advance();

        return Source[start..Position];
    }

    /// <summary>
    ///     读取带转义的字符串
    /// </summary>
    public string ReadEscapedString(char quote)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd && Peek() != quote)
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd) break;

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }

        if (!IsAtEnd && Peek() == quote) Advance();

        return sb.ToString();
    }

    /// <summary>
    ///     保存当前位置
    /// </summary>
    public TextPosition SavePosition()
    {
        return new TextPosition(Position, Line, Column);
    }

    /// <summary>
    ///     恢复到指定位置
    /// </summary>
    public void RestorePosition(TextPosition position)
    {
        Position = position.Position;
        Line = position.Line;
        Column = position.Column;
    }

    /// <summary>
    ///     提取指定范围的子字符串
    /// </summary>
    public string Slice(int start, int length)
    {
        return Source.Substring(start, Math.Min(length, Source.Length - start));
    }

    private static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
}