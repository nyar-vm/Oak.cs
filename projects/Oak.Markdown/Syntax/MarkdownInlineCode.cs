namespace Oak.Markdown.Syntax;

/// <summary>
///     行内代码节点
/// </summary>
public sealed record MarkdownInlineCode : MarkdownNode
{
    public MarkdownInlineCode(string content)
    {
        Content = content;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.InlineCode;

    /// <summary>
    ///     代码内容
    /// </summary>
    public string Content { get; init; } = string.Empty;
}