using System.Globalization;
using Oak.Diagnostics;
using Oak.Parsing;

namespace Oak.Yaml;

/// <summary>
///     YAML 解析器，将词法单元流解析为 YamlValue 树
/// </summary>
public sealed class YamlParser
{
    private DiagnosticSink? _diagnostics;
    private int _position;
    private IReadOnlyList<YamlToken> _tokens = [];

    private YamlToken Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    /// <summary>
    ///     解析 YAML 文本
    /// </summary>
    /// <param name="source">YAML 源文本</param>
    /// <param name="diagnostics">诊断收集器</param>
    /// <returns>解析结果</returns>
    public ParseResult<YamlValue> Parse(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new YamlLexer();
        _tokens = lexer.Tokenize(source, diagnostics);
        _position = 0;
        _diagnostics = diagnostics;

        var value = ParseDocument();

        if (_diagnostics is not null && _diagnostics.HasErrors) return ParseResult<YamlValue>.Fail(_diagnostics.Errors);

        return ParseResult<YamlValue>.Ok(value, _diagnostics?.Messages);
    }

    private YamlToken Advance()
    {
        var token = Current;
        if (_position < _tokens.Count - 1) _position++;
        return token;
    }

    private bool Match(YamlTokenType type)
    {
        if (Current.Type == type)
        {
            Advance();
            return true;
        }

        return false;
    }

    private YamlValue ParseDocument()
    {
        if (Current.Type == YamlTokenType.DocumentStart) Advance();

        var value = ParseBlock(0);

        if (Current.Type == YamlTokenType.DocumentEnd) Advance();

        return value;
    }

    private YamlValue ParseBlock(int minIndent)
    {
        SkipNewlines();

        if (Current.Type == YamlTokenType.Dash) return ParseSequence(minIndent);

        if (Current.Type == YamlTokenType.Key) return ParseMapping(minIndent);

        return ParseInlineValue();
    }

    private YamlMapping ParseMapping(int minIndent)
    {
        var properties = new List<(string Key, YamlValue Value)>();

        while (Current.Type == YamlTokenType.Key && Current.Indent >= minIndent)
        {
            var keyToken = Advance();
            var key = keyToken.Text.Trim();

            SkipNewlines();

            if (Current.Type == YamlTokenType.Indent && Current.Indent > keyToken.Indent)
            {
                var indent = Current.Indent;
                Advance();
                SkipNewlines();
                var value = ParseBlock(indent);
                properties.Add((key, value));
            }
            else if ((Current.Type == YamlTokenType.Key && Current.Indent > keyToken.Indent) ||
                     (Current.Type == YamlTokenType.Dash && Current.Indent > keyToken.Indent))
            {
                var value = ParseBlock(Current.Indent);
                properties.Add((key, value));
            }
            else
            {
                if (Current.Type == YamlTokenType.Indent) Advance();
                var value = ParseInlineValue();
                properties.Add((key, value));
            }

            SkipNewlines();
        }

        return new YamlMapping(properties.ToArray());
    }

    private YamlSequence ParseSequence(int minIndent)
    {
        var items = new List<YamlValue>();

        while (Current.Type == YamlTokenType.Dash && Current.Indent >= minIndent)
        {
            var dashToken = Advance();
            SkipNewlines();

            if (Current.Type == YamlTokenType.Indent && Current.Indent > dashToken.Indent)
            {
                var indent = Current.Indent;
                Advance();
                SkipNewlines();
                var item = ParseBlock(indent);
                items.Add(item);
            }
            else if ((Current.Type == YamlTokenType.Key && Current.Indent > dashToken.Indent) ||
                     (Current.Type == YamlTokenType.Dash && Current.Indent > dashToken.Indent))
            {
                var item = ParseBlock(Current.Indent);
                items.Add(item);
            }
            else if (Current.Type == YamlTokenType.Key)
            {
                var item = ParseMapping(dashToken.Indent + 2);
                items.Add(item);
            }
            else if (Current.Type == YamlTokenType.Dash && Current.Indent == dashToken.Indent)
            {
                items.Add(YamlNull.Instance);
            }
            else
            {
                var item = ParseInlineValue();
                items.Add(item);
            }

            SkipNewlines();
        }

        return new YamlSequence(items.ToArray());
    }

    private YamlValue ParseInlineValue()
    {
        switch (Current.Type)
        {
            case YamlTokenType.FlowMapStart:
                return ParseFlowMapping();
            case YamlTokenType.FlowSeqStart:
                return ParseFlowSequence();
            case YamlTokenType.Null:
                Advance();
                return YamlNull.Instance;
            case YamlTokenType.Boolean:
                return ParseBoolean();
            case YamlTokenType.Number:
                return ParseNumber();
            case YamlTokenType.String:
                var strToken = Advance();
                return new YamlString(Unquote(strToken.Text));
            case YamlTokenType.EndOfFile:
                return YamlNull.Instance;
            default:
                return YamlNull.Instance;
        }
    }

    private YamlBoolean ParseBoolean()
    {
        var token = Advance();
        var value = token.Text.ToLowerInvariant() switch
        {
            "true" or "yes" or "on" => true,
            "false" or "no" or "off" => false,
            _ => false
        };
        return new YamlBoolean(value);
    }

    private YamlNumber ParseNumber()
    {
        var token = Advance();
        var value = double.Parse(token.Text, CultureInfo.InvariantCulture);
        return new YamlNumber(value);
    }

    private YamlMapping ParseFlowMapping()
    {
        Advance();

        var properties = new List<(string Key, YamlValue Value)>();

        SkipNewlines();

        if (Current.Type != YamlTokenType.FlowMapEnd)
        {
            properties.Add(ParseFlowProperty());

            while (Match(YamlTokenType.FlowComma))
            {
                SkipNewlines();
                if (Current.Type == YamlTokenType.FlowMapEnd) break;
                properties.Add(ParseFlowProperty());
            }
        }

        SkipNewlines();

        if (Current.Type == YamlTokenType.FlowMapEnd) Advance();

        return new YamlMapping(properties.ToArray());
    }

    private (string Key, YamlValue Value) ParseFlowProperty()
    {
        var keyToken = Current;
        var key = keyToken.Type == YamlTokenType.Key
            ? Advance().Text.Trim()
            : keyToken.Type == YamlTokenType.String
                ? Unquote(Advance().Text)
                : Advance().Text;

        if (Current.Type == YamlTokenType.Colon) Advance();
        if (Current.Type == YamlTokenType.Indent) Advance();

        SkipNewlines();

        var value = ParseInlineValue();
        return (key, value);
    }

    private YamlSequence ParseFlowSequence()
    {
        Advance();

        var items = new List<YamlValue>();

        SkipNewlines();

        if (Current.Type != YamlTokenType.FlowSeqEnd)
        {
            items.Add(ParseInlineValue());

            while (Match(YamlTokenType.FlowComma))
            {
                SkipNewlines();
                if (Current.Type == YamlTokenType.FlowSeqEnd) break;
                items.Add(ParseInlineValue());
            }
        }

        SkipNewlines();

        if (Current.Type == YamlTokenType.FlowSeqEnd) Advance();

        return new YamlSequence(items.ToArray());
    }

    private void SkipNewlines()
    {
        while (Current.Type is YamlTokenType.Newline or YamlTokenType.Comment) Advance();
    }

    private static string Unquote(string text)
    {
        if (text.Length >= 2)
            if ((text.StartsWith('"') && text.EndsWith('"')) ||
                (text.StartsWith('\'') && text.EndsWith('\'')))
                return text[1..^1];

        return text;
    }
}