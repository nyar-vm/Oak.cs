using Oak.Syntax;

namespace Oak.Markdown.Syntax;

/// <summary>
///     Markdown 语法高亮器
/// </summary>
public sealed class MarkdownSyntaxHighlighter
{
    /// <summary>
    ///     对 Markdown 源码进行语法高亮
    /// </summary>
    public IReadOnlyList<HighlightSpan> Highlight(string source)
    {
        var lexer = new MarkdownLexer();
        var tokens = lexer.Tokenize(source);
        var spans = new List<HighlightSpan>(tokens.Count);
        var offset = 0;

        foreach (var token in tokens)
        {
            spans.Add(new HighlightSpan
            {
                Kind = HighlightKind.Other,
                Offset = offset,
                Length = token.Width
            });

            offset += token.Width;
        }

        return spans;
    }
}
