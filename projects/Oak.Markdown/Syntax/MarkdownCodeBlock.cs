namespace Oak.Markdown.Syntax;

/// <summary>
///     代码块节点
/// </summary>
public sealed record MarkdownCodeBlock : MarkdownNode
{
    public MarkdownCodeBlock(string? language, string content)
    {
        Language = language;
        Content = content;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.CodeBlock;

    /// <summary>
    ///     语言标识符
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    ///     代码内容
    /// </summary>
    public string Content { get; init; } = string.Empty;
}