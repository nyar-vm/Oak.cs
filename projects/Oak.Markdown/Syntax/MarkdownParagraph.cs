namespace Oak.Markdown.Syntax;

/// <summary>
///     段落节点
/// </summary>
public sealed record MarkdownParagraph : MarkdownNode
{
    public MarkdownParagraph(IReadOnlyList<MarkdownNode> children)
    {
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Paragraph;

    /// <summary>
    ///     段落内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}