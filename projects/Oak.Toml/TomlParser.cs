using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Toml.Ast;

namespace Oak.Toml;

public sealed class TomlParser
{
    private DiagnosticSink? _diagnostics;
    private string _source = string.Empty;
    private int _position;
    private int _line;
    private int _column;

    public TomlParseResult Parse(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var root = new TomlTable();
        var currentTable = root;
        var tables = new Dictionary<string, TomlTable> { [""] = root };

        while (!IsAtEnd())
        {
            SkipWhitespaceAndNewlines();

            if (IsAtEnd()) break;

            if (Peek() == '#')
            {
                SkipComment();
                continue;
            }

            if (Peek() == '\n' || Peek() == '\r')
            {
                SkipNewline();
                continue;
            }

            if (Peek() == '[')
            {
                currentTable = ParseTableHeader(root, tables);
                continue;
            }

            ParseKeyValue(currentTable);
        }

        return new TomlParseResult { Root = root, Diagnostics = _diagnostics?.Messages ?? [] };
    }

    #region 基础工具

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
        while (!IsAtEnd() && Peek() is ' ' or '\t') Advance();
    }

    private void SkipWhitespaceAndNewlines()
    {
        while (!IsAtEnd() && Peek() is ' ' or '\t' or '\n' or '\r') Advance();
    }

    private void SkipNewline()
    {
        if (Peek() == '\r') Advance();
        if (Peek() == '\n') Advance();
    }

    private void SkipComment()
    {
        while (!IsAtEnd() && Peek() != '\n') Advance();
    }

    private void ExpectNewlineOrEnd()
    {
        SkipWhitespace();

        if (IsAtEnd()) return;

        if (Peek() == '#')
        {
            SkipComment();
            return;
        }

        if (Peek() == '\n' || Peek() == '\r')
        {
            SkipNewline();
            return;
        }

        _diagnostics?.AddWarning(
            string.Empty,
            default,
            "TOML001",
            "期望换行或文件结尾");
    }

    #endregion

    #region 表头解析

    private TomlTable ParseTableHeader(TomlTable root, Dictionary<string, TomlTable> tables)
    {
        var isArrayTable = Peek() == '[' && PeekNext() == '[';

        if (isArrayTable)
        {
            Advance();
            Advance();
        }
        else
        {
            Advance();
        }

        SkipWhitespace();

        var key = ParseKey();

        SkipWhitespace();

        if (isArrayTable)
        {
            if (Peek() == ']' && PeekNext() == ']')
            {
                Advance();
                Advance();
            }
            else
            {
                _diagnostics?.AddError(
                    string.Empty,
                    default,
                    "TOML002",
                    "期望 ']]'");
            }
        }
        else
        {
            if (Peek() == ']')
            {
                Advance();
            }
            else
            {
                _diagnostics?.AddError(
                    string.Empty,
                    default,
                    "TOML003",
                    "期望 ']'");
            }
        }

        ExpectNewlineOrEnd();

        var parts = key.Split('.');
        var current = root;

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();

            if (i == parts.Length - 1)
            {
                var table = new TomlTable { Name = key, IsArrayTable = isArrayTable };

                if (!current.Tables.ContainsKey(part))
                {
                    current.Tables[part] = table;
                }
                else if (isArrayTable)
                {
                    current.Tables[part] = table;
                }

                tables[key] = table;
                current = table;
            }
            else
            {
                if (!current.Tables.TryGetValue(part, out var child))
                {
                    child = new TomlTable { Name = string.Join('.', parts[..(i + 1)]) };
                    current.Tables[part] = child;
                }

                current = child;
            }
        }

        return current;
    }

    #endregion

    #region 键值对解析

    private void ParseKeyValue(TomlTable table)
    {
        var key = ParseKey();

        SkipWhitespace();

        if (Peek() != '=')
        {
            _diagnostics?.AddError(
                string.Empty,
                default,
                "TOML004",
                "期望 '='");
            return;
        }

        Advance();
        SkipWhitespace();

        var value = ParseValue();

        table.Entries[key] = value;

        ExpectNewlineOrEnd();
    }

    private string ParseKey()
    {
        if (Peek() == '"')
        {
            return ParseQuotedKey('"');
        }

        if (Peek() == '\'')
        {
            return ParseQuotedKey('\'');
        }

        var start = _position;

        while (!IsAtEnd() && Peek() is not ('=' or '.' or ' ' or '\t' or '\n' or '\r' or '#' or '['))
        {
            Advance();
        }

        return _source[start.._position].Trim();
    }

    private string ParseQuotedKey(char quote)
    {
        Advance();

        var start = _position;

        while (!IsAtEnd() && Peek() != quote)
        {
            if (Peek() == '\\') Advance();
            Advance();
        }

        var content = _source[start.._position];

        if (!IsAtEnd()) Advance();

        return content;
    }

    #endregion

    #region 值解析

    private TomlValue ParseValue()
    {
        var c = Peek();

        if (c == '"')
        {
            return ParseBasicString();
        }

        if (c == '\'')
        {
            return ParseLiteralString();
        }

        if (c == '[')
        {
            return ParseArray();
        }

        if (c == '{')
        {
            return ParseInlineTable();
        }

        if (char.IsDigit(c) || c == '-' || c == '+' || c == 'i' || c == 'n')
        {
            return ParseScalarValue();
        }

        _diagnostics?.AddError(
            string.Empty,
            default,
            "TOML005",
            $"意外的字符 '{c}'");

        Advance();
        return new TomlValue { Type = TomlValueType.String, RawValue = string.Empty };
    }

    private TomlValue ParseScalarValue()
    {
        var start = _position;

        if (Peek() == 't' && _position + 3 < _source.Length && _source[_position..(_position + 4)] == "true")
        {
            _position += 4;
            _column += 4;
            return new TomlValue { Type = TomlValueType.Boolean, RawValue = true };
        }

        if (Peek() == 'f' && _position + 4 < _source.Length && _source[_position..(_position + 5)] == "false")
        {
            _position += 5;
            _column += 5;
            return new TomlValue { Type = TomlValueType.Boolean, RawValue = false };
        }

        while (!IsAtEnd() && Peek() is not (' ' or '\t' or '\n' or '\r' or '#' or ',' or ']' or '}'))
        {
            Advance();
        }

        var text = _source[start.._position];

        if (text.Contains(':') || text.Contains('T') || text.StartsWith("inf", StringComparison.OrdinalIgnoreCase) || text.StartsWith("nan", StringComparison.OrdinalIgnoreCase))
        {
            if (text is "inf" or "+inf" or "-inf" or "infinity" or "+infinity" or "-infinity")
            {
                return new TomlValue { Type = TomlValueType.Float, RawValue = text.StartsWith('-') ? double.NegativeInfinity : double.PositiveInfinity };
            }

            if (text is "nan" or "+nan" or "-nan")
            {
                return new TomlValue { Type = TomlValueType.Float, RawValue = double.NaN };
            }

            return new TomlValue { Type = TomlValueType.DateTime, RawValue = text };
        }

        if (text.Contains('.') || text.Contains('e') || text.Contains('E'))
        {
            if (double.TryParse(text.Replace("_", ""), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
            {
                return new TomlValue { Type = TomlValueType.Float, RawValue = floatVal };
            }
        }

        var intText = text.Replace("_", "");

        if (intText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (long.TryParse(intText[2..], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var hexVal))
            {
                return new TomlValue { Type = TomlValueType.Integer, RawValue = hexVal };
            }
        }

        if (intText.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var octVal = Convert.ToInt64(intText[2..], 8);
                return new TomlValue { Type = TomlValueType.Integer, RawValue = octVal };
            }
            catch (FormatException)
            {
                // 回退到字符串
            }
        }

        if (intText.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var binVal = Convert.ToInt64(intText[2..], 2);
                return new TomlValue { Type = TomlValueType.Integer, RawValue = binVal };
            }
            catch (FormatException)
            {
                // 回退到字符串
            }
        }

        if (long.TryParse(intText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var intVal))
        {
            return new TomlValue { Type = TomlValueType.Integer, RawValue = intVal };
        }

        return new TomlValue { Type = TomlValueType.String, RawValue = text };
    }

    #endregion

    #region 字符串解析

    private TomlValue ParseBasicString()
    {
        if (Peek() == '"' && PeekNext() == '"' && _position + 2 < _source.Length && _source[_position + 2] == '"')
        {
            return ParseMultilineBasicString();
        }

        Advance();

        var sb = new System.Text.StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\')
            {
                Advance();
                sb.Append(ParseEscapeSequence());
                continue;
            }

            sb.Append(Advance());
        }

        if (!IsAtEnd()) Advance();

        return new TomlValue { Type = TomlValueType.String, RawValue = sb.ToString() };
    }

    private TomlValue ParseMultilineBasicString()
    {
        Advance();
        Advance();
        Advance();

        if (Peek() == '\n') Advance();
        if (Peek() == '\r') { Advance(); if (Peek() == '\n') Advance(); }

        var sb = new System.Text.StringBuilder();

        while (!IsAtEnd())
        {
            if (Peek() == '"' && PeekNext() == '"' && _position + 2 < _source.Length && _source[_position + 2] == '"')
            {
                Advance();
                Advance();
                Advance();
                break;
            }

            if (Peek() == '\\')
            {
                if (PeekNext() == '\n' || PeekNext() == '\r')
                {
                    Advance();

                    if (Peek() == '\r') Advance();
                    if (Peek() == '\n') Advance();

                    SkipWhitespace();
                    continue;
                }

                Advance();
                sb.Append(ParseEscapeSequence());
                continue;
            }

            sb.Append(Advance());
        }

        return new TomlValue { Type = TomlValueType.String, RawValue = sb.ToString() };
    }

    private TomlValue ParseLiteralString()
    {
        if (Peek() == '\'' && PeekNext() == '\'' && _position + 2 < _source.Length && _source[_position + 2] == '\'')
        {
            return ParseMultilineLiteralString();
        }

        Advance();

        var start = _position;

        while (!IsAtEnd() && Peek() != '\'')
        {
            Advance();
        }

        var content = _source[start.._position];

        if (!IsAtEnd()) Advance();

        return new TomlValue { Type = TomlValueType.String, RawValue = content };
    }

    private TomlValue ParseMultilineLiteralString()
    {
        Advance();
        Advance();
        Advance();

        if (Peek() == '\n') Advance();
        if (Peek() == '\r') { Advance(); if (Peek() == '\n') Advance(); }

        var sb = new System.Text.StringBuilder();

        while (!IsAtEnd())
        {
            if (Peek() == '\'' && PeekNext() == '\'' && _position + 2 < _source.Length && _source[_position + 2] == '\'')
            {
                Advance();
                Advance();
                Advance();
                break;
            }

            sb.Append(Advance());
        }

        return new TomlValue { Type = TomlValueType.String, RawValue = sb.ToString() };
    }

    private char ParseEscapeSequence()
    {
        if (IsAtEnd()) return '\\';

        var c = Advance();

        return c switch
        {
            'b' => '\b',
            'f' => '\f',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            '\\' => '\\',
            '"' => '"',
            'u' => ParseUnicodeEscape(4),
            'U' => ParseUnicodeEscape(8),
            _ => c
        };
    }

    private char ParseUnicodeEscape(int digits)
    {
        var hex = new char[digits];

        for (var i = 0; i < digits; i++)
        {
            if (IsAtEnd()) return '\0';
            hex[i] = Advance();
        }

        var code = int.Parse(new string(hex), System.Globalization.NumberStyles.HexNumber);
        return (char)code;
    }

    #endregion

    #region 数组解析

    private TomlValue ParseArray()
    {
        Advance();
        SkipWhitespaceAndNewlines();

        var items = new List<TomlValue>();

        while (!IsAtEnd() && Peek() != ']')
        {
            SkipWhitespaceAndNewlines();

            if (Peek() == '#')
            {
                SkipComment();
                SkipWhitespaceAndNewlines();
                continue;
            }

            if (Peek() == ']') break;

            items.Add(ParseValue());

            SkipWhitespaceAndNewlines();

            if (Peek() == '#')
            {
                SkipComment();
                SkipWhitespaceAndNewlines();
            }

            if (Peek() == ',')
            {
                Advance();
                SkipWhitespaceAndNewlines();
                continue;
            }

            if (Peek() == '#')
            {
                SkipComment();
                SkipWhitespaceAndNewlines();
            }
        }

        if (!IsAtEnd()) Advance();

        return new TomlValue { Type = TomlValueType.Array, RawValue = items.ToArray() };
    }

    #endregion

    #region 内联表解析

    private TomlValue ParseInlineTable()
    {
        Advance();
        SkipWhitespace();

        var entries = new Dictionary<string, TomlValue>();

        while (!IsAtEnd() && Peek() != '}')
        {
            SkipWhitespace();

            if (Peek() == '}') break;

            var key = ParseKey();

            SkipWhitespace();

            if (Peek() != '=')
            {
                _diagnostics?.AddError(
                    string.Empty,
                    default,
                    "TOML006",
                    "内联表中期望 '='");
                break;
            }

            Advance();
            SkipWhitespace();

            var value = ParseValue();
            entries[key] = value;

            SkipWhitespace();

            if (Peek() == ',')
            {
                Advance();
                SkipWhitespace();
            }
        }

        if (!IsAtEnd()) Advance();

        return new TomlValue { Type = TomlValueType.InlineTable, RawValue = entries };
    }

    #endregion
}
