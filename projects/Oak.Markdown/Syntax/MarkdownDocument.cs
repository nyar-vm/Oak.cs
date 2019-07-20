namespace Oak.Markdown.Syntax;

/// <summary>
///     Markdown 文档根节点
/// </summary>
public sealed record MarkdownDocument : MarkdownNode
{
    public MarkdownDocument(IReadOnlyList<MarkdownNode> children)
    {
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Document;

    /// <summary>
    ///     子节点列表
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}