using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Typescript.Lexer;

/// <summary>
///     TypeScript 词法分析器
/// </summary>
public sealed class TsLexer
{
    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        "+", "-", "*", "/", "%", "**",
        "=", "==", "===", "!=", "!==", "<", ">", "<=", ">=",
        "+=", "-=", "*=", "/=", "%=", "**=",
        "&&", "||", "!",
        "&", "|", "^", "~", "<<", ">>", ">>>",
        "&=", "|=", "^=", "<<=", ">>=", ">>>=",
        "=>", "->", "::", "??", "??=",
        "?.", "?", ":"
    };

    private static readonly HashSet<char> Delimiters = ['(', ')', '{', '}', '[', ']', ',', ';', '.'];

    private DiagnosticSink? _diagnostics;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private ISource _source = StringSource.Empty;

    /// <summary>
    ///     创建 TypeScript 词法分析器
    /// </summary>
    public TsLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     将源代码文本转换为词法单元序列
    /// </summary>
    public IReadOnlyList<TsToken> Tokenize(string source)
    {
        _source = new StringSource(source);
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics ??= new DiagnosticSink();

        SkipHashbang();

        var tokens = new List<TsToken>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();

            if (IsAtEnd()) break;

            var token = ScanToken();

            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new TsToken(TsTokenType.Eof, string.Empty, _line, _column));
        return tokens;
    }

    private bool IsAtEnd() => _position >= _source.Length;

    private char Peek() => IsAtEnd() ? '\0' : _source[_position];

    private char PeekNext() => Peek(1);

    private char Peek(int offset)
    {
        var index = _position + offset;
        return index >= _source.Length ? '\0' : _source[index];
    }

    private char Advance()
    {
        var c = _source[_position];
        _position++;

        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return c;
    }

    private TsToken? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;
        var c = Peek();

        if (c is '"' or '\'') return ScanString(startLine, startColumn);
        if (c == '`') return ScanTemplateString(startLine, startColumn);
        if (char.IsDigit(c)) return ScanNumber(startLine, startColumn);
        if (c == '_' || c == '$' || char.IsLetter(c)) return ScanIdentifierOrKeyword(startLine, startColumn);
        if (IsOperatorStart(c)) return ScanOperatorOrPunctuation(startLine, startColumn);

        if (Delimiters.Contains(c))
        {
            Advance();
            return new TsToken(TsTokenType.Delimiter, c.ToString(), startLine, startColumn);
        }

        Advance();
        _diagnostics?.AddError(
            string.Empty,
            default,
            "OAK1001",
            $"意外的字符 '{c}'");

        return null;
    }

    private TsToken ScanString(int startLine, int startColumn)
    {
        var quote = Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != quote)
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd()) break;

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '"' => '"', '\'' => '\'',
                    '0' => '\0', _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }

        if (IsAtEnd())
            _diagnostics?.AddError(string.Empty,
                default,
                "OAK1002", "未闭合的字符串字面量");
        else
            Advance();

        return new TsToken(TsTokenType.String, sb.ToString(), startLine, startColumn);
    }

    private TsToken ScanTemplateString(int startLine, int startColumn)
    {
        Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '`')
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd()) break;

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '`' => '`', '$' => '$',
                    '0' => '\0', _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }

        if (IsAtEnd())
            _diagnostics?.AddError(string.Empty,
                default,
                "OAK1002", "未闭合的模板字符串");
        else
            Advance();

        return new TsToken(TsTokenType.TemplateString, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描数字字面量，支持十进制、十六进制、二进制、八进制及 BigInt
    /// </summary>
    private TsToken ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (Peek() == '0')
        {
            var next = PeekNext();

            if (next is 'x' or 'X')
            {
                Advance();
                Advance();
                sb.Append("0x");
                while (!IsAtEnd() && IsHexDigit(Peek())) sb.Append(Advance());

                if (!IsAtEnd() && Peek() == 'n')
                {
                    sb.Append(Advance());
                    return new TsToken(TsTokenType.BigInt, sb.ToString(), startLine, startColumn);
                }

                return new TsToken(TsTokenType.Number, sb.ToString(), startLine, startColumn);
            }

            if (next is 'b' or 'B')
            {
                Advance();
                Advance();
                sb.Append("0b");
                while (!IsAtEnd() && IsBinaryDigit(Peek())) sb.Append(Advance());

                if (!IsAtEnd() && Peek() == 'n')
                {
                    sb.Append(Advance());
                    return new TsToken(TsTokenType.BigInt, sb.ToString(), startLine, startColumn);
                }

                return new TsToken(TsTokenType.Number, sb.ToString(), startLine, startColumn);
            }

            if (next is 'o' or 'O')
            {
                Advance();
                Advance();
                sb.Append("0o");
                while (!IsAtEnd() && IsOctalDigit(Peek())) sb.Append(Advance());

                if (!IsAtEnd() && Peek() == 'n')
                {
                    sb.Append(Advance());
                    return new TsToken(TsTokenType.BigInt, sb.ToString(), startLine, startColumn);
                }

                return new TsToken(TsTokenType.Number, sb.ToString(), startLine, startColumn);
            }
        }

        while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());

        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
        {
            sb.Append(Advance());
            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            sb.Append(Advance());
            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) sb.Append(Advance());
            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        }

        if (!IsAtEnd() && (Peek() == 'f' || Peek() == 'F' || Peek() == 'i' || Peek() == 'I'
                           || Peek() == 'u' || Peek() == 'U'))
        {
            sb.Append(Advance());
            return new TsToken(TsTokenType.Number, sb.ToString(), startLine, startColumn);
        }

        if (!IsAtEnd() && Peek() == 'n')
        {
            sb.Append(Advance());
            return new TsToken(TsTokenType.BigInt, sb.ToString(), startLine, startColumn);
        }

        return new TsToken(TsTokenType.Number, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     判断字符是否为十六进制数字
    /// </summary>
    private static bool IsHexDigit(char c) =>
        char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';

    /// <summary>
    ///     判断字符是否为二进制数字
    /// </summary>
    private static bool IsBinaryDigit(char c) => c is '0' or '1';

    /// <summary>
    ///     判断字符是否为八进制数字
    /// </summary>
    private static bool IsOctalDigit(char c) => c is >= '0' and <= '7';

    private TsToken ScanIdentifierOrKeyword(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || Peek() == '$' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
        var text = sb.ToString();

        if (TsKeywords.All.Contains(text))
        {
            if (text is "true" or "false" or "null")
                return new TsToken(TsTokenType.Literal, text, startLine, startColumn);
            return new TsToken(TsTokenType.Keyword, text, startLine, startColumn);
        }

        return new TsToken(TsTokenType.Identifier, text, startLine, startColumn);
    }

    private static bool IsOperatorStart(char c) =>
        c is '+' or '-' or '*' or '/' or '%' or '=' or '!' or '<' or '>' or '&' or '|' or '^' or '~' or '?' or ':';

    private TsToken ScanOperatorOrPunctuation(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd())
        {
            var candidate = sb.ToString() + Peek();
            if (Operators.Contains(candidate)) sb.Append(Advance());
            else break;
        }

        var op = sb.ToString();
        if (op is ":") return new TsToken(TsTokenType.Punctuation, op, startLine, startColumn);
        return new TsToken(TsTokenType.Operator, op, startLine, startColumn);
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            switch (Peek())
            {
                case ' ' or '\t' or '\r' or '\n':
                    Advance();
                    break;
                case '/':
                    if (PeekNext() == '/') SkipLineComment();
                    else if (PeekNext() == '*') SkipBlockComment();
                    else return;
                    break;
                default:
                    return;
            }
        }
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n') Advance();
    }

    private void SkipBlockComment()
    {
        Advance();
        Advance();
        var startLine = _line;
        var startColumn = _column;
        var depth = 1;

        while (!IsAtEnd() && depth > 0)
        {
            if (Peek() == '/' && PeekNext() == '*') { Advance(); Advance(); depth++; }
            else if (Peek() == '*' && PeekNext() == '/') { Advance(); Advance(); depth--; }
            else Advance();
        }

        if (depth > 0)
            _diagnostics?.AddError(string.Empty,
                default,
                "OAK1003", "未闭合的块注释");
    }

    /// <summary>
    ///     跳过 Hashbang 注释（以 #! 开头的行）
    /// </summary>
    private void SkipHashbang()
    {
        if (_source is ['#', '!', ..])
        {
            while (!IsAtEnd() && Peek() != '\n') Advance();
        }
    }
}
