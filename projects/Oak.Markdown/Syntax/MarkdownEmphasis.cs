namespace Oak.Markdown.Syntax;

/// <summary>
///     强调节点（斜体）
/// </summary>
public sealed record MarkdownEmphasis : MarkdownNode
{
    public MarkdownEmphasis(IReadOnlyList<MarkdownNode> children)
    {
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Emphasis;

    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}