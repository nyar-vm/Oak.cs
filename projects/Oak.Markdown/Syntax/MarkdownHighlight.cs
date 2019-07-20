namespace Oak.Markdown.Syntax;

/// <summary>
///     高亮/标记节点 ==text==
/// </summary>
public sealed record MarkdownHighlight : MarkdownNode
{
    public MarkdownHighlight(IReadOnlyList<MarkdownNode> children)
    {
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Highlight;

    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}