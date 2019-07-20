using System.Globalization;
using Oak.Parsing;

namespace Oak.DejaVu.Expressions;

/// <summary>
///     表达式词法分析器
/// </summary>
public sealed class ExpressionLexer
{
    private readonly string _source;
    private int _position;

    /// <summary>
    ///     创建表达式词法分析器
    /// </summary>
    /// <param name="source">表达式源码</param>
    public ExpressionLexer(string source)
    {
        _source = source;
        _position = 0;
    }

    /// <summary>
    ///     将表达式源码分词
    /// </summary>
    public List<ExpressionToken> Tokenize()
    {
        var tokens = new List<ExpressionToken>();

        while (_position < _source.Length)
        {
            SkipWhitespace();

            if (_position >= _source.Length) break;

            var token = ReadToken();
            if (token != null) tokens.Add(token);
        }

        return tokens;
    }

    private void SkipWhitespace()
    {
        while (_position < _source.Length && char.IsWhiteSpace(_source[_position])) _position++;
    }

    private ExpressionToken? ReadToken()
    {
        var ch = _source[_position];

        if (ch is '"' or '\'') return ReadString();

        if (ch is >= '0' and <= '9' || (ch == '.' && _position + 1 < _source.Length && char.IsDigit(_source[_position + 1]))) return ReadNumber();

        if (ch == '+') return CreateToken(ExpressionTokenType.Plus, "+");
        if (ch == '-') return CreateToken(ExpressionTokenType.Minus, "-");
        if (ch == '*') return CreateToken(ExpressionTokenType.Multiply, "*");
        if (ch == '/') return CreateToken(ExpressionTokenType.Divide, "/");
        if (ch == '%') return CreateToken(ExpressionTokenType.Modulo, "%");
        if (ch == '(') return CreateToken(ExpressionTokenType.LeftParen, "(");
        if (ch == ')') return CreateToken(ExpressionTokenType.RightParen, ")");
        if (ch == '[') return CreateToken(ExpressionTokenType.LeftBracket, "[");
        if (ch == ']') return CreateToken(ExpressionTokenType.RightBracket, "]");
        if (ch == ',') return CreateToken(ExpressionTokenType.Comma, ",");
        if (ch == ':') return CreateToken(ExpressionTokenType.Colon, ":");
        if (ch == '.') return CreateToken(ExpressionTokenType.Dot, ".");

        if (ch == '=' && Peek(1) == '=') return CreateToken(ExpressionTokenType.Equal, "==");

        if (ch == '!' && Peek(1) == '=') return CreateToken(ExpressionTokenType.NotEqual, "!=");

        if (ch == '<' && Peek(1) == '=') return CreateToken(ExpressionTokenType.LessThanOrEqual, "<=");

        if (ch == '>' && Peek(1) == '=') return CreateToken(ExpressionTokenType.GreaterThanOrEqual, ">=");

        if (ch == '<') return CreateToken(ExpressionTokenType.LessThan, "<");

        if (ch == '>') return CreateToken(ExpressionTokenType.GreaterThan, ">");

        if (ch == '&' && Peek(1) == '&') return CreateToken(ExpressionTokenType.And, "&&");

        if (ch == '|' && Peek(1) == '|') return CreateToken(ExpressionTokenType.Or, "||");

        if (ch == '|' && Peek(1) == '>') return CreateToken(ExpressionTokenType.Pipe, "|>");

        if (ch == '!') return CreateToken(ExpressionTokenType.Not, "!");

        if (char.IsLetter(ch) || ch == '_') return ReadIdentifierOrKeyword();

        throw new ParseException($"Unexpected character: {ch}");
    }

    private ExpressionToken CreateToken(ExpressionTokenType type, string text)
    {
        _position += text.Length;
        return new ExpressionToken(type, text);
    }

    private char Peek(int offset)
    {
        var index = _position + offset;
        return index < _source.Length ? _source[index] : '\0';
    }

    private ExpressionToken ReadString()
    {
        var quote = _source[_position];
        _position++;
        var start = _position;

        while (_position < _source.Length && _source[_position] != quote)
            if (_source[_position] == '\\' && _position + 1 < _source.Length)
                _position += 2;
            else
                _position++;

        var value = _source[start.._position];
        _position++; // 跳过结束引号

        return new ExpressionToken(ExpressionTokenType.String, value);
    }

    private ExpressionToken ReadNumber()
    {
        var start = _position;
        var hasDecimal = false;

        while (_position < _source.Length &&
               (char.IsDigit(_source[_position]) || _source[_position] == '.'))
        {
            if (_source[_position] == '.')
            {
                if (hasDecimal) break;
                hasDecimal = true;
            }

            _position++;
        }

        var text = _source[start.._position];
        var value = double.Parse(text, CultureInfo.InvariantCulture);

        return new ExpressionToken(ExpressionTokenType.Number, value);
    }

    private ExpressionToken ReadIdentifierOrKeyword()
    {
        var start = _position;

        while (_position < _source.Length &&
               (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
            _position++;

        var text = _source[start.._position];

        return text.ToLowerInvariant() switch
        {
            "true" => new ExpressionToken(ExpressionTokenType.Boolean, true),
            "false" => new ExpressionToken(ExpressionTokenType.Boolean, false),
            _ => new ExpressionToken(ExpressionTokenType.Identifier, text)
        };
    }
}