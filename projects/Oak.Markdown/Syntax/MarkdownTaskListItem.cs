namespace Oak.Markdown.Syntax;

/// <summary>
///     任务列表项节点
/// </summary>
public sealed record MarkdownTaskListItem : MarkdownNode
{
    public MarkdownTaskListItem(bool isChecked, IReadOnlyList<MarkdownNode> children)
    {
        IsChecked = isChecked;
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.TaskListItem;

    /// <summary>
    ///     是否已勾选
    /// </summary>
    public bool IsChecked { get; init; }

    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}