using Oak.Json;
using Oak.Syntax;

namespace Oak.Json.Syntax;

/// <summary>
///     JSON 语法高亮器
/// </summary>
public sealed class JsonSyntaxHighlighter
{
    /// <summary>
    ///     对 JSON 源码进行语法高亮
    /// </summary>
    public IReadOnlyList<HighlightSpan> Highlight(string source)
    {
        var lexer = new JsonLexer();
        var tokens = lexer.Tokenize(source);
        var spans = new List<HighlightSpan>(tokens.Count);
        var offset = 0;

        foreach (var token in tokens)
        {
            if (token.Type == JsonTokenType.EndOfFile)
            {
                break;
            }

            // 跳过空白字符直到下一个 Token
            while (offset < source.Length && char.IsWhiteSpace(source[offset]))
            {
                offset++;
            }

            var kind = token.Type switch
            {
                JsonTokenType.Number => HighlightKind.Number,
                JsonTokenType.String => HighlightKind.String,
                JsonTokenType.True or JsonTokenType.False or JsonTokenType.Null => HighlightKind.Keyword,
                _ => HighlightKind.Other
            };

            spans.Add(new HighlightSpan
            {
                Kind = kind,
                Offset = offset,
                Length = token.Text.Length
            });

            offset += token.Text.Length;
        }

        return spans;
    }
}
