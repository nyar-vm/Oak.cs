using System.Globalization;
using Oak.Diagnostics;

namespace Oak.Yaml;

/// <summary>
///     YAML 词法分析器
/// </summary>
public sealed class YamlLexer
{
    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    /// <summary>
    ///     执行词法分析
    /// </summary>
    /// <param name="source">YAML 源文本</param>
    /// <param name="diagnostics">诊断收集器</param>
    /// <returns>词法单元列表</returns>
    public IReadOnlyList<YamlToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<YamlToken>();

        while (!IsAtEnd())
        {
            var token = ScanToken();
            if (token.Type != YamlTokenType.Invalid) tokens.Add(token);
        }

        tokens.Add(new YamlToken(YamlTokenType.EndOfFile, string.Empty, _line, _column));
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

    private YamlToken ScanToken()
    {
        if (Peek() == '\n')
        {
            Advance();
            return ScanLineStart();
        }

        if (Peek() == '\r')
        {
            Advance();
            if (Peek() == '\n') Advance();
            return ScanLineStart();
        }

        if (char.IsWhiteSpace(Peek())) return ScanIndentation();

        if (Peek() == '#') return ScanComment();

        if (Peek() == '-' && PeekNext() == '-' && PeekAt(2) == '-') return ScanDocumentStart();

        if (Peek() == '.' && PeekNext() == '.' && PeekAt(2) == '.') return ScanDocumentEnd();

        if (Peek() == '-' && _column == 1) return ScanDash();

        if (Peek() == '{')
        {
            var line = _line;
            var column = _column;
            Advance();
            return new YamlToken(YamlTokenType.FlowMapStart, "{", line, column);
        }

        if (Peek() == '}')
        {
            var line = _line;
            var column = _column;
            Advance();
            return new YamlToken(YamlTokenType.FlowMapEnd, "}", line, column);
        }

        if (Peek() == '[')
        {
            var line = _line;
            var column = _column;
            Advance();
            return new YamlToken(YamlTokenType.FlowSeqStart, "[", line, column);
        }

        if (Peek() == ']')
        {
            var line = _line;
            var column = _column;
            Advance();
            return new YamlToken(YamlTokenType.FlowSeqEnd, "]", line, column);
        }

        if (Peek() == ',')
        {
            var line = _line;
            var column = _column;
            Advance();
            return new YamlToken(YamlTokenType.FlowComma, ",", line, column);
        }

        if (Peek() == '!' && _column <= 2) return ScanTag();

        if (Peek() == '&') return ScanAnchor();

        if (Peek() == '*') return ScanAlias();

        return ScanValue();
    }

    private char PeekAt(int offset)
    {
        var index = _position + offset;
        return index >= _source.Length ? '\0' : _source[index];
    }

    private YamlToken ScanLineStart()
    {
        var indent = 0;

        while (!IsAtEnd() && (Peek() == ' ' || Peek() == '\t'))
        {
            indent++;
            Advance();
        }

        if (IsAtEnd() || Peek() == '\n' || Peek() == '\r')
            return new YamlToken(YamlTokenType.Newline, string.Empty, _line, 1, indent);

        if (Peek() == '#') return ScanComment();

        if (Peek() == '-' && PeekNext() == '-' && PeekAt(2) == '-') return ScanDocumentStart();

        if (Peek() == '.' && PeekNext() == '.' && PeekAt(2) == '.') return ScanDocumentEnd();

        if (Peek() == '-' && (PeekNext() == ' ' || PeekNext() == '\t'))
        {
            var line = _line;
            var column = _column;
            Advance();
            return new YamlToken(YamlTokenType.Dash, "-", line, column, indent);
        }

        return ScanKeyOrValue(indent);
    }

    private YamlToken ScanIndentation()
    {
        var line = _line;
        var column = _column;
        var indent = 0;

        while (!IsAtEnd() && (Peek() == ' ' || Peek() == '\t'))
        {
            indent++;
            Advance();
        }

        return new YamlToken(YamlTokenType.Indent, string.Empty, line, column, indent);
    }

    private YamlToken ScanComment()
    {
        var line = _line;
        var column = _column;
        Advance();

        var start = _position;
        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r') Advance();

        var text = _source[start.._position];
        return new YamlToken(YamlTokenType.Comment, text, line, column);
    }

    private YamlToken ScanDocumentStart()
    {
        var line = _line;
        var column = _column;
        Advance();
        Advance();
        Advance();
        return new YamlToken(YamlTokenType.DocumentStart, "---", line, column);
    }

    private YamlToken ScanDocumentEnd()
    {
        var line = _line;
        var column = _column;
        Advance();
        Advance();
        Advance();
        return new YamlToken(YamlTokenType.DocumentEnd, "...", line, column);
    }

    private YamlToken ScanDash()
    {
        var line = _line;
        var column = _column;

        if (PeekNext() == ' ' || PeekNext() == '\t')
        {
            Advance();
            return new YamlToken(YamlTokenType.Dash, "-", line, column);
        }

        return ScanValue();
    }

    private YamlToken ScanTag()
    {
        var line = _line;
        var column = _column;
        Advance();

        var start = _position;
        while (!IsAtEnd() && !char.IsWhiteSpace(Peek()) && Peek() != ':' && Peek() != ',') Advance();

        var text = _source[(start - 1).._position];
        return new YamlToken(YamlTokenType.Tag, text, line, column);
    }

    private YamlToken ScanAnchor()
    {
        var line = _line;
        var column = _column;
        Advance();

        var start = _position;
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_' || Peek() == '-')) Advance();

        var text = _source[(start - 1).._position];
        return new YamlToken(YamlTokenType.Anchor, text, line, column);
    }

    private YamlToken ScanAlias()
    {
        var line = _line;
        var column = _column;
        Advance();

        var start = _position;
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_' || Peek() == '-')) Advance();

        var text = _source[(start - 1).._position];
        return new YamlToken(YamlTokenType.Alias, text, line, column);
    }

    private YamlToken ScanKeyOrValue(int indent)
    {
        var line = _line;
        var column = _column;
        var start = _position;

        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r' && Peek() != ':' && Peek() != '#')
        {
            Advance();
        }

        if (Peek() == ':')
        {
            var text = _source[start.._position].TrimEnd();
            Advance();
            return new YamlToken(YamlTokenType.Key, text, line, column, indent);
        }

        return ClassifyValue(_source[start.._position].TrimEnd(), line, column, indent);
    }

    private YamlToken ScanValue()
    {
        var line = _line;
        var column = _column;
        var start = _position;

        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r' && Peek() != '#' && Peek() != ',' && Peek() != '}' && Peek() != ']')
        {
            if (Peek() == ':' && (PeekNext() == ' ' || PeekNext() == '\t' || PeekNext() == '\n' || PeekNext() == '\r' || _position + 1 >= _source.Length))
            {
                var keyText = _source[start.._position].TrimEnd();
                Advance();
                return new YamlToken(YamlTokenType.Key, keyText, line, column);
            }

            Advance();
        }

        var text = _source[start.._position].TrimEnd();
        return ClassifyValue(text, line, column);
    }

    private YamlToken ClassifyValue(string text, int line, int column, int indent = 0)
    {
        if (string.IsNullOrEmpty(text)) return new YamlToken(YamlTokenType.Null, text, line, column, indent);

        if (text is "null" or "~" or "Null" or "NULL")
            return new YamlToken(YamlTokenType.Null, text, line, column, indent);

        if (text is "true" or "True" or "TRUE" or "false" or "False" or "FALSE")
            return new YamlToken(YamlTokenType.Boolean, text, line, column, indent);

        if (text is "yes" or "Yes" or "YES" or "no" or "No" or "NO" or "on" or "On" or "ON" or "off" or "Off" or "OFF")
            return new YamlToken(YamlTokenType.Boolean, text, line, column, indent);

        if (double.TryParse(text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out _))
            return new YamlToken(YamlTokenType.Number, text, line, column, indent);

        if (text.StartsWith('"') || text.StartsWith('\''))
            return new YamlToken(YamlTokenType.String, text, line, column, indent);

        return new YamlToken(YamlTokenType.String, text, line, column, indent);
    }
}