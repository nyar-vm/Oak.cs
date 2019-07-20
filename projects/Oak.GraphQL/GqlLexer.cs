using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.GraphQL;

public sealed class GqlLexer
{
    private static readonly Dictionary<string, GqlTokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["query"] = GqlTokenType.Query,
        ["mutation"] = GqlTokenType.Mutation,
        ["subscription"] = GqlTokenType.Subscription,
        ["fragment"] = GqlTokenType.Fragment,
        ["on"] = GqlTokenType.On,
        ["type"] = GqlTokenType.Type,
        ["input"] = GqlTokenType.Input,
        ["interface"] = GqlTokenType.Interface,
        ["enum"] = GqlTokenType.Enum,
        ["union"] = GqlTokenType.Union,
        ["scalar"] = GqlTokenType.Scalar,
        ["schema"] = GqlTokenType.Schema,
        ["extend"] = GqlTokenType.Extend,
        ["directive"] = GqlTokenType.Directive,
        ["repeatable"] = GqlTokenType.Repeatable,
        ["implements"] = GqlTokenType.Implements,
        ["null"] = GqlTokenType.Null,
        ["true"] = GqlTokenType.True,
        ["false"] = GqlTokenType.False
    };

    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public IReadOnlyList<GqlToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<GqlToken>();

        while (!IsAtEnd())
        {
            SkipIgnored();

            if (IsAtEnd()) break;

            var token = ScanToken();
            if (token.Type != GqlTokenType.Invalid) tokens.Add(token);
        }

        tokens.Add(new GqlToken(GqlTokenType.EndOfFile, string.Empty, _line, _column));
        return tokens;
    }

    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    }

    private char Peek()
    {
        return IsAtEnd() ? '\0' : _source[_position];
    }

    private char PeekNext()
    {
        return _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
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

    private void SkipIgnored()
    {
        while (!IsAtEnd())
        {
            var c = Peek();

            if (char.IsWhiteSpace(c) || c == ',' || c == '\n' || c == '\r' || c == '\t')
            {
                Advance();
            }
            else if (c == '#')
            {
                while (!IsAtEnd() && Peek() != '\n') Advance();
            }
            else
            {
                break;
            }
        }
    }

    private GqlToken ScanBlockString(int line, int column)
    {
        Advance();
        Advance();
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd())
        {
            if (Peek() == '"' && PeekNext() == '"' && _position + 2 < _source.Length && _source[_position + 2] == '"')
            {
                Advance();
                Advance();
                Advance();
                return new GqlToken(GqlTokenType.StringValue, sb.ToString(), line, column);
            }

            if (Peek() == '\\' && PeekNext() == '"' && _position + 2 < _source.Length && _source[_position + 3] < _source.Length)
            {
                if (_source[_position + 2] == '"' && _source[_position + 3] == '"')
                {
                    Advance();
                    Advance();
                    Advance();
                    Advance();
                    sb.Append("\"\"\"");
                    continue;
                }
            }

            sb.Append(Advance());
        }

        return new GqlToken(GqlTokenType.StringValue, sb.ToString(), line, column);
    }

    private GqlToken ScanToken()
    {
        var line = _line;
        var column = _column;
        var c = Peek();

        switch (c)
        {
            case '{':
                Advance();
                return new GqlToken(GqlTokenType.LeftBrace, "{", line, column);
            case '}':
                Advance();
                return new GqlToken(GqlTokenType.RightBrace, "}", line, column);
            case '(':
                Advance();
                return new GqlToken(GqlTokenType.LeftParen, "(", line, column);
            case ')':
                Advance();
                return new GqlToken(GqlTokenType.RightParen, ")", line, column);
            case '[':
                Advance();
                return new GqlToken(GqlTokenType.LeftBracket, "[", line, column);
            case ']':
                Advance();
                return new GqlToken(GqlTokenType.RightBracket, "]", line, column);
            case '!':
                Advance();
                return new GqlToken(GqlTokenType.Exclamation, "!", line, column);
            case '$':
                Advance();
                return new GqlToken(GqlTokenType.Dollar, "$", line, column);
            case '@':
                Advance();
                return new GqlToken(GqlTokenType.At, "@", line, column);
            case '&':
                Advance();
                return new GqlToken(GqlTokenType.Ampersand, "&", line, column);
            case '|':
                Advance();
                return new GqlToken(GqlTokenType.Pipe, "|", line, column);
            case '=':
                Advance();
                return new GqlToken(GqlTokenType.Equals, "=", line, column);
            case ':':
                Advance();
                return new GqlToken(GqlTokenType.Colon, ":", line, column);
            case '.':
                if (PeekNext() == '.' && _position + 2 < _source.Length && _source[_position + 2] == '.')
                {
                    Advance();
                    Advance();
                    Advance();
                    return new GqlToken(GqlTokenType.Spread, "...", line, column);
                }

                _diagnostics?.AddError(string.Empty, default,
                    "GQL001", "意外的字符 '.'");
                Advance();
                return new GqlToken(GqlTokenType.Invalid, ".", line, column);
            case '"':
                if (PeekNext() == '"' && _position + 2 < _source.Length && _source[_position + 2] == '"')
                    return ScanBlockString(line, column);
                return ScanString(line, column);
            default:
            {
                if (c == '-' || char.IsDigit(c)) return ScanNumber(line, column);

                if (c == '_' || char.IsLetter(c)) return ScanName(line, column);

                _diagnostics?.AddError(string.Empty, default,
                    "GQL002", $"意外的字符 '{c}'");
                Advance();
                return new GqlToken(GqlTokenType.Invalid, c.ToString(), line, column);
            }
        }
    }

    private GqlToken ScanString(int line, int column)
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd()) break;

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    '/' => '/',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd()) Advance();

        return new GqlToken(GqlTokenType.StringValue, sb.ToString(), line, column);
    }

    private GqlToken ScanNumber(int line, int column)
    {
        var start = _position;

        if (Peek() == '-') Advance();

        while (!IsAtEnd() && char.IsDigit(Peek())) Advance();

        var isFloat = false;

        if (!IsAtEnd() && Peek() == '.')
        {
            isFloat = true;
            Advance();
            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            isFloat = true;
            Advance();
            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) Advance();
            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        var text = _source[start.._position];
        return new GqlToken(isFloat ? GqlTokenType.FloatValue : GqlTokenType.IntValue, text, line, column);
    }

    private GqlToken ScanName(int line, int column)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_')) sb.Append(Advance());

        var text = sb.ToString();

        if (Keywords.TryGetValue(text, out var keywordType))
            return new GqlToken(keywordType, text, line, column);

        return new GqlToken(GqlTokenType.Name, text, line, column);
    }
}
