using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Valkyrie.Lexer;

namespace Oak.Valkyrie.Parser;

/// <summary>
///     Parser 的 Token 源，封装 GreenLeafNode 列表的遍历操作
/// </summary>
internal sealed class TokenStream
{
    private const string ParserErrorCode = "PARSE";
    private const string DefaultFilePath = "";

    private readonly IReadOnlyList<GreenLeafNode> _tokens;
    private readonly DiagnosticSink? _diagnostics;
    private int _position;

    /// <summary>
    ///     用 Token 列表初始化
    /// </summary>
    public TokenStream(IReadOnlyList<GreenLeafNode> tokens, DiagnosticSink? diagnostics = null)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _diagnostics = diagnostics;
        _position = 0;
    }

    /// <summary>
    ///     诊断接收器
    /// </summary>
    internal DiagnosticSink? Diagnostics => _diagnostics;

    /// <summary>
    ///     是否已到达 Token 流末尾（跳过 Eof 后视为结束）
    /// </summary>
    public bool IsAtEnd()
    {
        return _position >= _tokens.Count || PeekKind() == ValkyrieTokenKind.Eos;
    }

    /// <summary>
    ///     获取当前 Token
    /// </summary>
    public GreenLeafNode Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    /// <summary>
    ///     当前在源中的位置
    /// </summary>
    public int Position => _position;

    /// <summary>
    ///     向前看指定偏移量的 Token（0 = 当前）
    /// </summary>
    public GreenLeafNode Peek(int offset = 0)
    {
        var index = _position + offset;
        if (index >= _tokens.Count)
        {
            return _tokens[^1];
        }

        return _tokens[index];
    }

    /// <summary>
    ///     向前看指定偏移量 Token 的文本
    /// </summary>
    public string PeekText(int offset = 0)
    {
        return Peek(offset).Text ?? string.Empty;
    }

    /// <summary>
    ///     向前看指定偏移量 Token 的 NodeKind
    /// </summary>
    public ValkyrieTokenKind PeekKind(int offset = 0)
    {
        return (ValkyrieTokenKind)Peek(offset).Kind.Value;
    }

    /// <summary>
    ///     检查当前 Token 是否为指定类型
    /// </summary>
    public bool Check(ValkyrieTokenKind kind)
    {
        return Current.Kind == kind.ToNodeKind();
    }

    /// <summary>
    ///     检查指定偏移的 Token 是否为指定类型
    /// </summary>
    public bool Check(ValkyrieTokenKind kind, int offset)
    {
        return Peek(offset).Kind == kind.ToNodeKind();
    }

    /// <summary>
    ///     检查当前 Token 是否为关键词
    /// </summary>
    public bool IsKeyword()
    {
        return Current.Kind.IsKeyword();
    }

    /// <summary>
    ///     检查当前 Token 是否为操作符
    /// </summary>
    public bool IsOperator()
    {
        return Current.Kind.IsOperator();
    }

    /// <summary>
    ///     检查指定偏移的 Token 是否为操作符
    /// </summary>
    public bool IsOperator(int offset)
    {
        return Peek(offset).Kind.IsOperator();
    }

    /// <summary>
    ///     检查当前 Token 是否为字面量
    /// </summary>
    public bool IsLiteral() => Current.Kind.IsLiteral();

    /// <summary>
    ///     消耗当前 Token 并前进一步，返回消耗的 Token
    /// </summary>
    public GreenLeafNode Advance()
    {
        if (!IsAtEnd())
        {
            _position++;
        }

        return _tokens[_position - 1];
    }

    /// <summary>
    ///     消耗当前 Token 并前进一步，返回消耗 Token 的文本
    /// </summary>
    public string AdvanceText()
    {
        return Advance().Text ?? string.Empty;
    }

    /// <summary>
    ///     检查当前 Token 的 Kind 是否匹配
    /// </summary>
    public bool Check(NodeKind kind)
    {
        if (IsAtEnd())
        {
            return false;
        }

        return Current.Kind == kind;
    }

    /// <summary>
    ///     检查当前 Token 的文本是否匹配（忽略大小写）
    /// </summary>
    public bool Check(string text)
    {
        if (IsAtEnd())
        {
            return false;
        }

        return string.Equals(Current.Text, text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     如果当前 Kind 匹配则消耗它并返回 true，否则返回 false
    /// </summary>
    public bool Match(ValkyrieTokenKind kind)
    {
        if (Check(kind))
        {
            Advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     期望当前 Token 是指定的 Kind，否则报告诊断并抛出异常
    /// </summary>
    public GreenLeafNode Expect(ValkyrieTokenKind kind)
    {
        if (Check(kind))
        {
            return Advance();
        }

        var message = $"期望 Token 类型 {kind}，但遇到 {Current.Kind} (\"{Current.Text}\")，位置 {_position}";
        _diagnostics?.AddError(DefaultFilePath, new TextSpan(_position, 1), ParserErrorCode, message);

        return Advance();
    }

    /// <summary>
    ///     如果当前 Token 文本匹配则消耗它并返回 true，否则返回 false
    /// </summary>
    public bool Match(string text)
    {
        if (Check(text))
        {
            Advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     如果当前 Kind 匹配则消耗它并返回 true，否则返回 false
    /// </summary>
    public bool Match(NodeKind kind, out GreenLeafNode token)
    {
        if (Check(kind))
        {
            token = Advance();
            return true;
        }

        token = default!;
        return false;
    }

    /// <summary>
    ///     期望当前 Token 是指定的 Kind，否则报告诊断并抛出异常
    /// </summary>
    public GreenLeafNode Expect(NodeKind kind)
    {
        if (Check(kind))
        {
            return Advance();
        }

        var message = $"期望 Token 类型 {kind}，但遇到 {Current.Kind} (\"{Current.Text}\")，位置 {_position}";
        _diagnostics?.AddError(DefaultFilePath, new TextSpan(_position, 1), ParserErrorCode, message);
        throw new InvalidOperationException(message);
    }

    /// <summary>
    ///     期望当前 Token 是 Keyword 且有指定文本，否则报告诊断并抛出异常
    /// </summary>
    public GreenLeafNode ExpectKeyword(string keyword)
    {
        if (IsKeyword() && Check(keyword))
        {
            return Advance();
        }

        var message = $"期望关键字 \"{keyword}\"，但遇到 \"{Current.Text}\"，位置 {_position}";
        _diagnostics?.AddError(DefaultFilePath, new TextSpan(_position, 1), ParserErrorCode, message);
        throw new InvalidOperationException(message);
    }

    /// <summary>
    ///     同步到下一个语句边界（错误恢复用）
    /// </summary>
    public void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if ((ValkyrieTokenKind)_tokens[_position - 1].Kind.Value == ValkyrieTokenKind.Semicolon)
            {
                return;
            }

            if (IsKeyword()
                || Check(ValkyrieTokenKind.BraceL)
                || Check(ValkyrieTokenKind.BraceR))
            {
                return;
            }

            Advance();
        }
    }

    /// <summary>
    ///     从解析错误中恢复：报告错误并同步到下一个语句边界
    /// </summary>
    public void RecoverFromError(string message)
    {
        _diagnostics?.AddError(DefaultFilePath, new TextSpan(_position, 1), ParserErrorCode, message);
        Synchronize();
    }
}
