namespace Oak.Markdown.Syntax;

/// <summary>
///     标题节点
/// </summary>
public sealed record MarkdownHeading : MarkdownNode
{
    public MarkdownHeading(int level, IReadOnlyList<MarkdownNode> children)
    {
        Level = level;
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Heading;

    /// <summary>
    ///     标题级别（1-6）
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    ///     标题内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}