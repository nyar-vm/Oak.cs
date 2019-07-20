using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Protobuf;

public sealed class ProtoLexer
{
    private static readonly Dictionary<string, ProtoTokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["syntax"] = ProtoTokenType.Syntax,
        ["package"] = ProtoTokenType.Package,
        ["import"] = ProtoTokenType.Import,
        ["option"] = ProtoTokenType.Option,
        ["message"] = ProtoTokenType.Message,
        ["enum"] = ProtoTokenType.Enum,
        ["service"] = ProtoTokenType.Service,
        ["rpc"] = ProtoTokenType.Rpc,
        ["returns"] = ProtoTokenType.Returns,
        ["stream"] = ProtoTokenType.Stream,
        ["repeated"] = ProtoTokenType.Repeated,
        ["optional"] = ProtoTokenType.Optional,
        ["required"] = ProtoTokenType.Required,
        ["oneof"] = ProtoTokenType.Oneof,
        ["map"] = ProtoTokenType.Map,
        ["reserved"] = ProtoTokenType.Reserved,
        ["extensions"] = ProtoTokenType.Extensions,
        ["extend"] = ProtoTokenType.Extend,
        ["group"] = ProtoTokenType.Group,
        ["true"] = ProtoTokenType.BoolLiteral,
        ["false"] = ProtoTokenType.BoolLiteral
    };

    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public IReadOnlyList<ProtoToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<ProtoToken>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();

            if (IsAtEnd()) break;

            var token = ScanToken();
            if (token.Type != ProtoTokenType.Invalid) tokens.Add(token);
        }

        tokens.Add(new ProtoToken(ProtoTokenType.EndOfFile, string.Empty, _line, _column));
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

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            var c = Peek();

            if (char.IsWhiteSpace(c))
            {
                Advance();
            }
            else if (c == '/' && PeekNext() == '/')
            {
                while (!IsAtEnd() && Peek() != '\n') Advance();
            }
            else if (c == '/' && PeekNext() == '*')
            {
                Advance();
                Advance();
                while (!IsAtEnd())
                {
                    if (Peek() == '*' && PeekNext() == '/')
                    {
                        Advance();
                        Advance();
                        break;
                    }

                    Advance();
                }
            }
            else
            {
                break;
            }
        }
    }

    private ProtoToken ScanToken()
    {
        var line = _line;
        var column = _column;
        var c = Peek();

        switch (c)
        {
            case '{':
                Advance();
                return new ProtoToken(ProtoTokenType.LeftBrace, "{", line, column);
            case '}':
                Advance();
                return new ProtoToken(ProtoTokenType.RightBrace, "}", line, column);
            case '[':
                Advance();
                return new ProtoToken(ProtoTokenType.LeftBracket, "[", line, column);
            case ']':
                Advance();
                return new ProtoToken(ProtoTokenType.RightBracket, "]", line, column);
            case '(':
                Advance();
                return new ProtoToken(ProtoTokenType.LeftParen, "(", line, column);
            case ')':
                Advance();
                return new ProtoToken(ProtoTokenType.RightParen, ")", line, column);
            case ';':
                Advance();
                return new ProtoToken(ProtoTokenType.Semicolon, ";", line, column);
            case ':':
                Advance();
                return new ProtoToken(ProtoTokenType.Colon, ":", line, column);
            case ',':
                Advance();
                return new ProtoToken(ProtoTokenType.Comma, ",", line, column);
            case '.':
                Advance();
                return new ProtoToken(ProtoTokenType.Dot, ".", line, column);
            case '=':
                Advance();
                return new ProtoToken(ProtoTokenType.Equals, "=", line, column);
            case '<':
                Advance();
                return new ProtoToken(ProtoTokenType.Lt, "<", line, column);
            case '>':
                Advance();
                return new ProtoToken(ProtoTokenType.Gt, ">", line, column);
            case '"':
                return ScanString(line, column);
            case '\'':
                return ScanSingleQuoteString(line, column);
            default:
            {
                if (c == '-' || c == '+' || c == '.' && char.IsDigit(PeekNext()) || char.IsDigit(c))
                    return ScanNumber(line, column);

                if (c == '_' || char.IsLetter(c)) return ScanIdentifier(line, column);

                _diagnostics?.AddError(string.Empty, default,
                    "PROTO001", $"意外的字符 '{c}'");
                Advance();
                return new ProtoToken(ProtoTokenType.Invalid, c.ToString(), line, column);
            }
        }
    }

    private ProtoToken ScanString(int line, int column)
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\')
            {
                Advance();
                if (!IsAtEnd()) sb.Append(Advance());
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd()) Advance();

        return new ProtoToken(ProtoTokenType.StringLiteral, sb.ToString(), line, column);
    }

    private ProtoToken ScanSingleQuoteString(int line, int column)
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '\'')
        {
            if (Peek() == '\\')
            {
                Advance();
                if (!IsAtEnd()) sb.Append(Advance());
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd()) Advance();

        return new ProtoToken(ProtoTokenType.StringLiteral, sb.ToString(), line, column);
    }

    private ProtoToken ScanNumber(int line, int column)
    {
        var start = _position;
        var isFloat = false;

        if (Peek() == '-' || Peek() == '+') Advance();

        if (Peek() == '0' && (PeekNext() == 'x' || PeekNext() == 'X'))
        {
            Advance();
            Advance();
            while (!IsAtEnd() && IsHexDigit(Peek())) Advance();
        }
        else
        {
            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();

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
        }

        var text = _source[start.._position];
        return new ProtoToken(isFloat ? ProtoTokenType.FloatLiteral : ProtoTokenType.IntLiteral, text, line, column);
    }

    private ProtoToken ScanIdentifier(int line, int column)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_')) sb.Append(Advance());

        var text = sb.ToString();

        if (Keywords.TryGetValue(text, out var keywordType))
            return new ProtoToken(keywordType, text, line, column);

        return new ProtoToken(ProtoTokenType.Identifier, text, line, column);
    }

    private static bool IsHexDigit(char c)
    {
        return char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
    }
}
