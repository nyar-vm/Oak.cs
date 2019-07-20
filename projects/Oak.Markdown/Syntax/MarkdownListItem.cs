namespace Oak.Markdown.Syntax;

/// <summary>
///     列表项节点
/// </summary>
public sealed record MarkdownListItem : MarkdownNode
{
    public MarkdownListItem(IReadOnlyList<MarkdownNode> children)
    {
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.ListItem;

    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}