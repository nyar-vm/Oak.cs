using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Lexing;

/// <summary>
///     词法分析器基类，提供通用扫描逻辑，产出 GreenLeafNode。
///     仅追踪字符偏移量（Position），不追踪行列。
///     行列信息由 LineIndex 根据 offset 按需计算。
/// </summary>
public abstract class LexerBase
{
    protected DiagnosticSink? Diagnostics;
    protected int Position;
    protected int Line = 1;
    protected int Column = 1;
    protected ISource Source = StringSource.Empty;

    protected LexerBase()
    {
    }

    protected LexerBase(ISource source, DiagnosticSink? diagnostics = null)
    {
        Source = source;
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     执行词法分析，产出 GreenLeafNode 列表
    /// </summary>
    public abstract IReadOnlyList<GreenLeafNode> Tokenize(string source);

    /// <summary>
    ///     是否已到达源代码末尾
    /// </summary>
    protected bool IsAtEnd()
    {
        return Position >= Source.Length;
    }

    /// <summary>
    ///     查看当前字符但不移动位置
    /// </summary>
    protected char Peek()
    {
        return IsAtEnd() ? '\0' : Source[Position];
    }

    /// <summary>
    ///     查看指定偏移处的字符
    /// </summary>
    protected char Peek(int offset)
    {
        var index = Position + offset;
        return index >= Source.Length ? '\0' : Source[index];
    }

    /// <summary>
    ///     查看下一个字符
    /// </summary>
    protected char PeekNext()
    {
        return Peek(1);
    }

    /// <summary>
    ///     前进一个字符并返回
    /// </summary>
    protected char Advance()
    {
        var c = Source[Position];
        Position++;
        return c;
    }

    /// <summary>
    ///     尝试匹配指定字符
    /// </summary>
    protected bool Match(char expected)
    {
        if (IsAtEnd() || Source[Position] != expected) return false;

        Advance();
        return true;
    }

    /// <summary>
    ///     跳过空白字符
    /// </summary>
    protected void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek())) Advance();
    }

    /// <summary>
    ///     从 ISource 执行词法分析
    /// </summary>
    public virtual IReadOnlyList<GreenLeafNode> Tokenize(ISource source)
    {
        Source = source;
        Reset();
        return Tokenize(source.Substring(new Range(0, source.Length)));
    }

    /// <summary>
    ///     重置内部状态
    /// </summary>
    protected void Reset()
    {
        Position = 0;
    }
}
