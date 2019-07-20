namespace Oak.Markdown.Syntax;

/// <summary>
///     HTML 块节点
/// </summary>
public sealed record MarkdownHtmlBlock : MarkdownNode
{
    public MarkdownHtmlBlock(string content)
    {
        Content = content;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.HtmlBlock;

    /// <summary>
    ///     HTML 内容
    /// </summary>
    public string Content { get; init; } = string.Empty;
}

/// <summary>
///     HTML 行内标签节点
/// </summary>
public sealed record MarkdownHtmlInline : MarkdownNode
{
    public MarkdownHtmlInline(string content)
    {
        Content = content;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.HtmlInline;

    /// <summary>
    ///     HTML 内容
    /// </summary>
    public string Content { get; init; } = string.Empty;
}