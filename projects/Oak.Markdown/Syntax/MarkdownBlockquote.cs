namespace Oak.Markdown.Syntax;

/// <summary>
///     引用块节点
/// </summary>
public sealed record MarkdownBlockquote : MarkdownNode
{
    public MarkdownBlockquote(IReadOnlyList<MarkdownNode> children)
    {
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Blockquote;

    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}