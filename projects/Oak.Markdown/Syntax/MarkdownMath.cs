namespace Oak.Markdown.Syntax;

/// <summary>
///     行内数学公式节点 $...$
/// </summary>
public sealed record MarkdownMathInline : MarkdownNode
{
    public MarkdownMathInline(string content)
    {
        Content = content;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.MathInline;

    /// <summary>
    ///     公式内容
    /// </summary>
    public string Content { get; init; } = string.Empty;
}

/// <summary>
///     块级数学公式节点 $$...$$
/// </summary>
public sealed record MarkdownMathBlock : MarkdownNode
{
    public MarkdownMathBlock(string content)
    {
        Content = content;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.MathBlock;

    /// <summary>
    ///     公式内容
    /// </summary>
    public string Content { get; init; } = string.Empty;
}