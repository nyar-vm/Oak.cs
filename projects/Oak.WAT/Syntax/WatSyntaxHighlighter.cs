using Oak.Syntax;

namespace Oak.Wat.Syntax;

/// <summary>
///     WAT（WebAssembly Text）语法高亮器
/// </summary>
public sealed class WatSyntaxHighlighter
{
    /// <summary>
    ///     对 WAT 源码进行语法高亮
    /// </summary>
    public IReadOnlyList<HighlightSpan> Highlight(string source)
    {
        var lexer = new WatLexer();
        var tokens = lexer.Tokenize(source);
        var spans = new List<HighlightSpan>(tokens.Count);
        var offset = 0;

        foreach (var token in tokens)
        {
            var kind = token.Type switch
            {
                WatTokenType.Keyword => HighlightKind.Keyword,
                WatTokenType.Opcode => HighlightKind.Keyword,
                WatTokenType.ValueType => HighlightKind.TypeName,
                WatTokenType.Number => HighlightKind.Number,
                WatTokenType.StringLiteral => HighlightKind.String,
                WatTokenType.Comment => HighlightKind.Comment,
                WatTokenType.Identifier => HighlightKind.Identifier,
                WatTokenType.Punctuation => HighlightKind.Delimiter,
                _ => HighlightKind.Other
            };
            var length = token.Value.Length;
            spans.Add(new HighlightSpan { Kind = kind, Offset = offset, Length = length });
            offset += length;
        }

        return spans;
    }
}
