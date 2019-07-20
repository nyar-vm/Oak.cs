namespace Oak.Markdown.Syntax;

/// <summary>
///     链接节点
/// </summary>
public sealed record MarkdownLink : MarkdownNode
{
    public MarkdownLink(string url, string? title, IReadOnlyList<MarkdownNode> children)
    {
        Url = url;
        Title = title;
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Link;

    /// <summary>
    ///     链接地址
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    ///     链接标题
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     显示内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}