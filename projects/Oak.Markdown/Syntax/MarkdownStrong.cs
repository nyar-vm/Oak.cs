namespace Oak.Markdown.Syntax;

/// <summary>
///     粗体节点
/// </summary>
public sealed record MarkdownStrong : MarkdownNode
{
    public MarkdownStrong(IReadOnlyList<MarkdownNode> children)
    {
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Strong;

    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}