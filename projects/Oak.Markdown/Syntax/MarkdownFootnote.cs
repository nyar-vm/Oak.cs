namespace Oak.Markdown.Syntax;

/// <summary>
///     脚注引用节点 [^1]
/// </summary>
public sealed record MarkdownFootnote : MarkdownNode
{
    public MarkdownFootnote(string label)
    {
        Label = label;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Footnote;

    /// <summary>
    ///     脚注标识
    /// </summary>
    public string Label { get; init; } = string.Empty;
}

/// <summary>
///     脚注定义节点 [^1]: text
/// </summary>
public sealed record MarkdownFootnoteDefinition : MarkdownNode
{
    public MarkdownFootnoteDefinition(string label, IReadOnlyList<MarkdownNode> children)
    {
        Label = label;
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.FootnoteDefinition;

    /// <summary>
    ///     脚注标识
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    ///     脚注内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}