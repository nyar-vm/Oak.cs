using System.Globalization;
using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Json;

/// <summary>
///     JSON 词法分析器
/// </summary>
public sealed class JsonLexer
{
    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    /// <summary>
    ///     执行词法分析
    /// </summary>
    /// <param name="source">JSON 源文本</param>
    /// <param name="diagnostics">诊断收集器</param>
    /// <returns>词法单元列表</returns>
    public IReadOnlyList<JsonToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<JsonToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();

            if (IsAtEnd()) break;

            var token = ScanToken();
            if (token.Type != JsonTokenType.Invalid) tokens.Add(token);
        }

        tokens.Add(new JsonToken(JsonTokenType.EndOfFile, string.Empty, _line, _column));
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

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek())) Advance();
    }

    private JsonToken ScanToken()
    {
        var line = _line;
        var column = _column;
        var c = Peek();

        switch (c)
        {
            case '{':
                Advance();
                return new JsonToken(JsonTokenType.LeftBrace, "{", line, column);
            case '}':
                Advance();
                return new JsonToken(JsonTokenType.RightBrace, "}", line, column);
            case '[':
                Advance();
                return new JsonToken(JsonTokenType.LeftBracket, "[", line, column);
            case ']':
                Advance();
                return new JsonToken(JsonTokenType.RightBracket, "]", line, column);
            case ',':
                Advance();
                return new JsonToken(JsonTokenType.Comma, ",", line, column);
            case ':':
                Advance();
                return new JsonToken(JsonTokenType.Colon, ":", line, column);
            case '"':
                return ScanString(line, column);
            default:
            {
                if (c == '-' || char.IsDigit(c)) return ScanNumber(line, column);

                if (c == 't') return ScanKeyword("true", JsonTokenType.True, line, column);

                if (c == 'f') return ScanKeyword("false", JsonTokenType.False, line, column);

                if (c == 'n') return ScanKeyword("null", JsonTokenType.Null, line, column);

                _diagnostics?.AddError(
                    string.Empty,
                    default,
                    "JSON001",
                    $"意外的字符 '{c}'");

                Advance();
                return new JsonToken(JsonTokenType.Invalid, c.ToString(), line, column);
            }
        }
    }

    private JsonToken ScanString(int line, int column)
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
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
                    'u' => ScanUnicodeEscape(),
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }

        if (!IsAtEnd()) Advance();

        return new JsonToken(JsonTokenType.String, sb.ToString(), line, column);
    }

    private char ScanUnicodeEscape()
    {
        var hex = new char[4];
        for (var i = 0; i < 4; i++)
        {
            if (IsAtEnd()) return '\0';
            hex[i] = Advance();
        }

        var code = int.Parse(new string(hex), NumberStyles.HexNumber);
        return (char)code;
    }

    private JsonToken ScanNumber(int line, int column)
    {
        var start = _position;

        if (Peek() == '-') Advance();

        while (!IsAtEnd() && char.IsDigit(Peek())) Advance();

        if (!IsAtEnd() && Peek() == '.')
        {
            Advance();

            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            Advance();

            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) Advance();

            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        var text = _source[start.._position];
        return new JsonToken(JsonTokenType.Number, text, line, column);
    }

    private JsonToken ScanKeyword(string keyword, JsonTokenType type, int line, int column)
    {
        foreach (var ch in keyword)
        {
            if (IsAtEnd() || Peek() != ch)
            {
                _diagnostics?.AddError(
                    string.Empty,
                    default,
                    "JSON002",
                    $"意外的字符，期望 '{keyword}'");

                return new JsonToken(JsonTokenType.Invalid, _source[column..(_position + 1)], line, column);
            }

            Advance();
        }

        return new JsonToken(type, keyword, line, column);
    }
}