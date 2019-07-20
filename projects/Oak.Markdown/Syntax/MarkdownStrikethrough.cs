namespace Oak.Markdown.Syntax;

/// <summary>
///     删除线节点
/// </summary>
public sealed record MarkdownStrikethrough : MarkdownNode
{
    public MarkdownStrikethrough(IReadOnlyList<MarkdownNode> children)
    {
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Strikethrough;

    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}