using System.Text;
using Oak.Data;
using Oak.Diagnostics;
using Oak.Parsing;
using TextReader = Oak.Text.TextReader;

namespace Oak.Von;

/// <summary>
///     Gon 配置格式解析器
/// </summary>
public sealed class GonParser : IStringParser<GonValue>, ISerdeFormat
{
    private readonly DiagnosticSink? _diagnostics;
    private TextReader _reader = new(string.Empty);

    public GonParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <inheritdoc />
    public string FormatName => "VON";

    /// <summary>
    ///     解析 Gon 文本（兼容旧接口，保留 TypeName/VariantName）
    /// </summary>
    public GonValue Parse(string source)
    {
        _reader = new TextReader(source);
        return ParseGonValueInternal();
    }

    /// <inheritdoc />
    public SerdeValue Deserialize(string source)
    {
        _reader = new TextReader(source);
        return ParseValueInternal();
    }

    /// <inheritdoc />
    public string Serialize(SerdeValue value)
    {
        return value.ToString();
    }

    #region GonValue 解析（保留 VON 特有元数据）

    private GonValue ParseGonValueInternal()
    {
        SkipWhitespaceAndComments();

        if (_reader.IsAtEnd) return GonValue.Null();

        var c = _reader.Peek();

        if (c == '{') return GonValue.Object(null, null, ParseObjectFields());

        if (c == '[') return GonValue.Array(ParseArrayElements());

        if (c == '"') return GonValue.String(ParseQuotedString());

        if (c == '-' || char.IsDigit(c)) return ParseGonNumber();

        if (IsIdentifierStart(c)) return ParseGonIdentifierValue();

        _diagnostics?.AddError(
            string.Empty,
            default,
            "OAK2001",
            $"意外的字符 '{c}'");

        _reader.Advance();
        return GonValue.Null();
    }

    private GonValue ParseGonIdentifierValue()
    {
        var identifier = ParseIdentifier();

        SkipWhitespaceAndComments();

        if (identifier == "true") return GonValue.Boolean(true);

        if (identifier == "false") return GonValue.Boolean(false);

        if (identifier == "null") return GonValue.Null();

        if (identifier is "inf" or "nan")
        {
            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2008",
                $"Von 格式不支持 '{identifier}'，属于语法错误");

            return GonValue.Null();
        }

        // VON 类型标注对象：Person { name: "Bob" }
        if (!_reader.IsAtEnd && _reader.Peek() == '{')
        {
            var fields = ParseObjectFields();
            return GonValue.Object(identifier, null, fields);
        }

        // VON 变体对象：Color Red { r: 255 }
        if (!_reader.IsAtEnd && IsIdentifierStart(_reader.Peek()))
        {
            var variantName = ParseIdentifier();
            SkipWhitespaceAndComments();

            if (!_reader.IsAtEnd && _reader.Peek() == '{')
            {
                var fields = ParseObjectFields();
                return GonValue.Object(identifier, variantName, fields);
            }

            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2009",
                $"标识符序列 '{identifier} {variantName}' 不是有效的值");

            return GonValue.Null();
        }

        _diagnostics?.AddError(
            string.Empty,
            default,
            "OAK2007",
            $"标识符 '{identifier}' 不是有效的值");

        return GonValue.Null();
    }

    private GonValue ParseGonNumber()
    {
        var isNegative = false;

        if (_reader.Peek() == '-')
        {
            isNegative = true;
            _reader.Advance();
        }

        var sb = new StringBuilder();

        while (!_reader.IsAtEnd && char.IsDigit(_reader.Peek())) sb.Append(_reader.Advance());

        var hasDecimalPoint = false;
        if (!_reader.IsAtEnd && _reader.Peek() == '.')
        {
            hasDecimalPoint = true;
            sb.Append(_reader.Advance());

            while (!_reader.IsAtEnd && char.IsDigit(_reader.Peek())) sb.Append(_reader.Advance());
        }

        if (!_reader.IsAtEnd && (_reader.Peek() == 'e' || _reader.Peek() == 'E'))
        {
            hasDecimalPoint = true;
            sb.Append(_reader.Advance());

            if (!_reader.IsAtEnd && (_reader.Peek() == '+' || _reader.Peek() == '-')) sb.Append(_reader.Advance());

            while (!_reader.IsAtEnd && char.IsDigit(_reader.Peek())) sb.Append(_reader.Advance());
        }

        var numberStr = sb.ToString();
        if (isNegative) numberStr = "-" + numberStr;

        if (hasDecimalPoint)
        {
            return GonValue.Decimal(numberStr);
        }

        return GonValue.Integer(numberStr);
    }

    private Dictionary<string, GonValue> ParseObjectFields()
    {
        _reader.Advance();

        var fields = new Dictionary<string, GonValue>();

        SkipWhitespaceAndComments();

        if (_reader.Peek() != '}')
        {
            ParseGonField(fields);

            while (true)
            {
                SkipWhitespaceAndComments();

                if (_reader.Peek() != ',') break;

                _reader.Advance();
                SkipWhitespaceAndComments();

                if (_reader.Peek() == '}') break;

                ParseGonField(fields);
            }
        }

        SkipWhitespaceAndComments();

        if (_reader.Peek() == '}')
            _reader.Advance();
        else
            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2002",
                "期望 '}'");

        return fields;
    }

    private void ParseGonField(Dictionary<string, GonValue> fields)
    {
        SkipWhitespaceAndComments();

        string fieldName;

        if (_reader.Peek() == '"')
            fieldName = ParseQuotedString();
        else
            fieldName = ParseIdentifier();

        SkipWhitespaceAndComments();

        if (_reader.Peek() == ':')
            _reader.Advance();
        else
            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2003",
                $"期望 ':'，但遇到 '{_reader.Peek()}'");

        var value = ParseGonValueInternal();
        fields[fieldName] = value;
    }

    private List<GonValue> ParseArrayElements()
    {
        _reader.Advance();

        var elements = new List<GonValue>();

        SkipWhitespaceAndComments();

        if (_reader.Peek() != ']')
        {
            elements.Add(ParseGonValueInternal());

            while (true)
            {
                SkipWhitespaceAndComments();

                if (_reader.Peek() != ',') break;

                _reader.Advance();
                SkipWhitespaceAndComments();

                if (_reader.Peek() == ']') break;

                elements.Add(ParseGonValueInternal());
            }
        }

        SkipWhitespaceAndComments();

        if (_reader.Peek() == ']')
            _reader.Advance();
        else
            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2004",
                "期望 ']'");

        return elements;
    }

    #endregion

    #region SerdeValue 解析（通用接口）

    private SerdeValue ParseValueInternal()
    {
        SkipWhitespaceAndComments();

        if (_reader.IsAtEnd) return SerdeValue.Null();

        var c = _reader.Peek();

        if (c == '{') return ParseObjectInternal();

        if (c == '[') return ParseArrayInternal();

        if (c == '"') return SerdeValue.String(ParseQuotedString());

        if (c == '-' || char.IsDigit(c)) return ParseNumberInternal();

        if (IsIdentifierStart(c)) return ParseIdentifierValueInternal();

        _diagnostics?.AddError(
            string.Empty,
            default,
            "OAK2001",
            $"意外的字符 '{c}'");

        _reader.Advance();
        return SerdeValue.Null();
    }

    private SerdeValue ParseObjectInternal()
    {
        _reader.Advance();

        var fields = new Dictionary<string, SerdeValue>();

        SkipWhitespaceAndComments();

        if (_reader.Peek() != '}')
        {
            ParseFieldInternal(fields);

            while (true)
            {
                SkipWhitespaceAndComments();

                if (_reader.Peek() != ',') break;

                _reader.Advance();
                SkipWhitespaceAndComments();

                if (_reader.Peek() == '}') break;

                ParseFieldInternal(fields);
            }
        }

        SkipWhitespaceAndComments();

        if (_reader.Peek() == '}')
            _reader.Advance();
        else
            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2002",
                "期望 '}'");

        return SerdeValue.Object(fields);
    }

    private void ParseFieldInternal(Dictionary<string, SerdeValue> fields)
    {
        SkipWhitespaceAndComments();

        string fieldName;

        if (_reader.Peek() == '"')
            fieldName = ParseQuotedString();
        else
            fieldName = ParseIdentifier();

        SkipWhitespaceAndComments();

        if (_reader.Peek() == ':')
            _reader.Advance();
        else
            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2003",
                $"期望 ':'，但遇到 '{_reader.Peek()}'");

        var value = ParseValueInternal();
        fields[fieldName] = value;
    }

    private SerdeValue ParseArrayInternal()
    {
        _reader.Advance();

        var elements = new List<SerdeValue>();

        SkipWhitespaceAndComments();

        if (_reader.Peek() != ']')
        {
            elements.Add(ParseValueInternal());

            while (true)
            {
                SkipWhitespaceAndComments();

                if (_reader.Peek() != ',') break;

                _reader.Advance();
                SkipWhitespaceAndComments();

                if (_reader.Peek() == ']') break;

                elements.Add(ParseValueInternal());
            }
        }

        SkipWhitespaceAndComments();

        if (_reader.Peek() == ']')
            _reader.Advance();
        else
            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2004",
                "期望 ']'");

        return SerdeValue.Array(elements);
    }

    private SerdeValue ParseNumberInternal()
    {
        var isNegative = false;

        if (_reader.Peek() == '-')
        {
            isNegative = true;
            _reader.Advance();
        }

        var sb = new StringBuilder();

        while (!_reader.IsAtEnd && char.IsDigit(_reader.Peek())) sb.Append(_reader.Advance());

        var hasDecimalPoint = false;
        if (!_reader.IsAtEnd && _reader.Peek() == '.')
        {
            hasDecimalPoint = true;
            sb.Append(_reader.Advance());

            while (!_reader.IsAtEnd && char.IsDigit(_reader.Peek())) sb.Append(_reader.Advance());
        }

        if (!_reader.IsAtEnd && (_reader.Peek() == 'e' || _reader.Peek() == 'E'))
        {
            hasDecimalPoint = true;
            sb.Append(_reader.Advance());

            if (!_reader.IsAtEnd && (_reader.Peek() == '+' || _reader.Peek() == '-')) sb.Append(_reader.Advance());

            while (!_reader.IsAtEnd && char.IsDigit(_reader.Peek())) sb.Append(_reader.Advance());
        }

        var numberStr = sb.ToString();
        if (isNegative) numberStr = "-" + numberStr;

        if (hasDecimalPoint)
        {
            return SerdeValue.Decimal(numberStr);
        }

        return SerdeValue.Integer(numberStr);
    }

    private SerdeValue ParseIdentifierValueInternal()
    {
        var identifier = ParseIdentifier();

        SkipWhitespaceAndComments();

        if (identifier == "true") return SerdeValue.Boolean(true);

        if (identifier == "false") return SerdeValue.Boolean(false);

        if (identifier == "null") return SerdeValue.Null();

        if (identifier is "inf" or "nan")
        {
            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2008",
                $"Von 格式不支持 '{identifier}'，属于语法错误");

            return SerdeValue.Null();
        }

        // VON 类型标注对象：Person { name: "Bob" }
        if (!_reader.IsAtEnd && _reader.Peek() == '{')
        {
            return ParseObjectInternal();
        }

        // VON 变体对象：Color Red { r: 255 }
        if (!_reader.IsAtEnd && IsIdentifierStart(_reader.Peek()))
        {
            var nextIdentifier = ParseIdentifier();
            SkipWhitespaceAndComments();

            if (!_reader.IsAtEnd && _reader.Peek() == '{')
            {
                return ParseObjectInternal();
            }

            _diagnostics?.AddError(
                string.Empty,
                default,
                "OAK2009",
                $"标识符序列 '{identifier} {nextIdentifier}' 不是有效的值");

            return SerdeValue.Null();
        }

        _diagnostics?.AddError(
            string.Empty,
            default,
            "OAK2007",
            $"标识符 '{identifier}' 不是有效的值");

        return SerdeValue.Null();
    }

    #endregion

    #region 通用辅助方法

    private string ParseIdentifier()
    {
        var sb = new StringBuilder();

        while (!_reader.IsAtEnd && IsIdentifierPart(_reader.Peek())) sb.Append(_reader.Advance());

        return sb.ToString();
    }

    private string ParseQuotedString()
    {
        _reader.Advance();

        var sb = new StringBuilder();

        while (!_reader.IsAtEnd && _reader.Peek() != '"')
            if (_reader.Peek() == '\\')
            {
                _reader.Advance();
                if (_reader.IsAtEnd) break;

                var escaped = _reader.Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(_reader.Advance());
            }

        if (!_reader.IsAtEnd) _reader.Advance();

        return sb.ToString();
    }

    private void SkipWhitespaceAndComments()
    {
        while (!_reader.IsAtEnd)
            if (char.IsWhiteSpace(_reader.Peek()))
            {
                _reader.SkipWhitespace();
            }
            else if (_reader.Peek() == '#')
            {
                if (_reader.Peek(1) == '>') return;
                _reader.SkipLineComment();
            }
            else if (_reader.Peek() == '<' && _reader.Peek(1) == '#')
            {
                _reader.SkipBlockComment();
            }
            else
            {
                break;
            }
    }

    private static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    #endregion
}
