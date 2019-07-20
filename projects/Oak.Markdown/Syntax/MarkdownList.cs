namespace Oak.Markdown.Syntax;

/// <summary>
///     列表节点
/// </summary>
public sealed record MarkdownList : MarkdownNode
{
    public MarkdownList(bool isOrdered, IReadOnlyList<MarkdownNode> items)
    {
        IsOrdered = isOrdered;
        Items = items;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.List;

    /// <summary>
    ///     是否为有序列表
    /// </summary>
    public bool IsOrdered { get; init; }

    /// <summary>
    ///     列表项（可为 MarkdownListItem 或 MarkdownTaskListItem）
    /// </summary>
    public IReadOnlyList<MarkdownNode> Items { get; init; } = [];
}