namespace Oak.DejaVu.Expressions;

/// <summary>
///     表达式令牌类型
/// </summary>
public enum ExpressionTokenType
{
    Number,
    String,
    Boolean,
    Identifier,
    Plus,
    Minus,
    Multiply,
    Divide,
    Modulo,
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    And,
    Or,
    Not,
    Pipe,
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    Comma,
    Dot,
    Colon
}

/// <summary>
///     表达式令牌
/// </summary>
public sealed class ExpressionToken
{
    /// <summary>
    ///     创建表达式令牌
    /// </summary>
    /// <param name="type">令牌类型</param>
    /// <param name="value">令牌值</param>
    public ExpressionToken(ExpressionTokenType type, object? value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    ///     令牌类型
    /// </summary>
    public ExpressionTokenType Type { get; }

    /// <summary>
    ///     值
    /// </summary>
    public object? Value { get; }
}

/// <summary>
///     令牌读取器
/// </summary>
public sealed class TokenReader
{
    private readonly List<ExpressionToken> _tokens;
    private int _position;

    /// <summary>
    ///     创建令牌读取器
    /// </summary>
    /// <param name="tokens">令牌列表</param>
    public TokenReader(List<ExpressionToken> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    /// <summary>
    ///     是否已结束
    /// </summary>
    public bool IsAtEnd => _position >= _tokens.Count;

    /// <summary>
    ///     当前令牌
    /// </summary>
    public ExpressionToken Current => _tokens[_position];

    /// <summary>
    ///     前进
    /// </summary>
    public ExpressionToken Advance()
    {
        return _tokens[_position++];
    }
}