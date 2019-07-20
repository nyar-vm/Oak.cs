namespace Oak.Markdown.Syntax;

/// <summary>
///     纯文本节点
/// </summary>
public sealed record MarkdownText : MarkdownNode
{
    public MarkdownText(string content)
    {
        Content = content;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Text;

    /// <summary>
    ///     文本内容
    /// </summary>
    public string Content { get; init; } = string.Empty;
}