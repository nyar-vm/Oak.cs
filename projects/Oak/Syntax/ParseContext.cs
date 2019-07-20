using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Syntax;

public delegate void RefParseAction<TLanguage, TContext>(ref ParseContext<TLanguage, TContext> ctx)
    where TLanguage : Language
    where TContext : ISyntaxContext;

public ref struct ParseContext<TLanguage, TContext>
    where TLanguage : Language
    where TContext : ISyntaxContext
{
    public ISource Source { get; }
    public int Position { get; set; }
    public TLanguage Language { get; }
    public TContext Context { get; }
    public DiagnosticSink Diagnostics { get; }

    public ParseContext(ISource source, TLanguage language, TContext context, DiagnosticSink diagnostics)
    {
        Source = source;
        Position = 0;
        Language = language;
        Context = context;
        Diagnostics = diagnostics;
    }

    public char Current => Position < Source.Length ? Source[Position] : '\0';

    public char Peek(int offset = 0)
    {
        var index = Position + offset;
        return index < Source.Length ? Source[index] : '\0';
    }

    public void Advance()
    {
        if (Position < Source.Length) Position++;
    }

    /// <summary>
    ///     跳转到指定字符，停在目标字符位置
    /// </summary>
    public void SkipTo(char target)
    {
        while (Position < Source.Length)
        {
            if (Source[Position] == target) return;
            Position++;
        }
    }

    /// <summary>
    ///     跳转到满足谓词的字符，停在满足条件的字符位置
    /// </summary>
    public void SkipTo(Func<char, bool> predicate)
    {
        while (Position < Source.Length)
        {
            if (predicate(Source[Position])) return;
            Position++;
        }
    }

    /// <summary>
    ///     跳转到指定节点类型。
    ///     当前为占位实现，因为 ParseContext 不具备词法单元感知能力，
    ///     NodeKind 用于语法树节点而非词法单元。
    /// </summary>
    public void SkipTo(NodeKind kind)
    {
        while (Position < Source.Length) Position++;
    }

    /// <summary>
    ///     尝试执行解析操作，若失败则报告诊断并跳转到恢复点
    /// </summary>
    public void Recover(RefParseAction<TLanguage, TContext> parseAction, string message)
    {
        var errorCountBefore = Diagnostics.Errors.Count;
        var positionBefore = Position;

        parseAction(ref this);

        if (Diagnostics.Errors.Count > errorCountBefore)
        {
            Diagnostics.AddError(string.Empty, GetSpanFrom(positionBefore), "OAK_RECOVER", message);
            SkipTo(c => c is ';' or '}' or '\n');
        }
    }

    /// <summary>
    ///     期望当前字符为指定字符，不匹配时报告错误诊断
    /// </summary>
    public bool Expect(char expected, string code, string message)
    {
        if (Current == expected) return true;

        Diagnostics.AddError(string.Empty, default, code, message);
        return false;
    }

    /// <summary>
    ///     期望当前字符满足谓词，不满足时报告错误诊断
    /// </summary>
    public bool Expect(Func<char, bool> predicate, string code, string message)
    {
        if (predicate(Current)) return true;

        Diagnostics.AddError(string.Empty, default, code, message);
        return false;
    }

    public TextSpan GetSpanFrom(int startPosition)
    {
        return default;
    }

    public string GetText(TextSpan span)
    {
        return Source.Substring(new Range(span.Start, span.End));
    }
}