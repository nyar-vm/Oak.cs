using System.Globalization;
using Oak.Diagnostics;
using Oak.Parsing;
using Oak.Syntax;

namespace Oak.Json;

/// <summary>
///     JSON 解析器，将词法单元流解析为 JsonValue 树
/// </summary>
public sealed class JsonParser
{
    private DiagnosticSink? _diagnostics;
    private int _position;
    private IReadOnlyList<JsonToken> _tokens = [];

    private JsonToken Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    /// <summary>
    ///     解析 JSON 文本
    /// </summary>
    /// <param name="source">JSON 源文本</param>
    /// <param name="diagnostics">诊断收集器</param>
    /// <returns>解析结果</returns>
    public ParseResult<JsonValue> Parse(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new JsonLexer();
        _tokens = lexer.Tokenize(source, diagnostics);
        _position = 0;
        _diagnostics = diagnostics;

        var value = ParseValue();

        if (_diagnostics is not null && _diagnostics.HasErrors) return ParseResult<JsonValue>.Fail(_diagnostics.Errors);

        return ParseResult<JsonValue>.Ok(value, _diagnostics?.Messages);
    }

    private JsonToken Advance()
    {
        var token = Current;
        if (_position < _tokens.Count - 1) _position++;
        return token;
    }

    private bool Match(JsonTokenType type)
    {
        if (Current.Type == type)
        {
            Advance();
            return true;
        }

        return false;
    }

    private JsonToken Expect(JsonTokenType type, string errorCode, string message)
    {
        if (Current.Type == type) return Advance();

        _diagnostics?.AddError(
            string.Empty,
            default,
            errorCode,
            $"{message}，实际遇到 {Current.Type}");

        return Current;
    }

    private JsonValue ParseValue()
    {
        return Current.Type switch
        {
            JsonTokenType.LeftBrace => ParseObject(),
            JsonTokenType.LeftBracket => ParseArray(),
            JsonTokenType.String => new JsonString(Advance().Text),
            JsonTokenType.Number => ParseNumber(),
            JsonTokenType.True => Advance() is var _ ? JsonBoolean.True : JsonBoolean.True,
            JsonTokenType.False => Advance() is var _ ? JsonBoolean.False : JsonBoolean.False,
            JsonTokenType.Null => Advance() is var _ ? JsonNull.Instance : JsonNull.Instance,
            _ => ParseUnexpectedValue()
        };
    }

    private JsonValue ParseUnexpectedValue()
    {
        _diagnostics?.AddError(
            string.Empty,
            default,
            "JSON003",
            $"意外的词法单元 {Current.Type}");

        Advance();
        return JsonNull.Instance;
    }

    private JsonNumber ParseNumber()
    {
        var token = Advance();
        var value = double.Parse(token.Text, CultureInfo.InvariantCulture);
        return new JsonNumber(value);
    }

    private JsonObject ParseObject()
    {
        Advance();

        var properties = new List<(string Key, JsonValue Value)>();

        if (Current.Type != JsonTokenType.RightBrace)
        {
            properties.Add(ParseProperty());

            while (Match(JsonTokenType.Comma)) properties.Add(ParseProperty());
        }

        Expect(JsonTokenType.RightBrace, "JSON004", "期望 '}'");
        return new JsonObject(properties.ToArray());
    }

    private (string Key, JsonValue Value) ParseProperty()
    {
        var keyToken = Expect(JsonTokenType.String, "JSON005", "期望属性名（字符串）");
        Expect(JsonTokenType.Colon, "JSON006", "期望 ':'");
        var value = ParseValue();
        return (keyToken.Text, value);
    }

    private JsonArray ParseArray()
    {
        Advance();

        var items = new List<JsonValue>();

        if (Current.Type != JsonTokenType.RightBracket)
        {
            items.Add(ParseValue());

            while (Match(JsonTokenType.Comma)) items.Add(ParseValue());
        }

        Expect(JsonTokenType.RightBracket, "JSON007", "期望 ']'");
        return new JsonArray(items.ToArray());
    }
}