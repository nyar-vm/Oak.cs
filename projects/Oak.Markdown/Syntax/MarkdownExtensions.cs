namespace Oak.Markdown.Syntax;

/// <summary>
///     软换行节点（段落内单个换行）
/// </summary>
public sealed record MarkdownSoftBreak : MarkdownNode
{
    public override MarkdownNodeType NodeType => MarkdownNodeType.SoftBreak;
}

/// <summary>
///     引用式链接定义节点 [id]: url "title"
/// </summary>
public sealed record MarkdownReferenceLinkDefinition : MarkdownNode
{
    public MarkdownReferenceLinkDefinition(string label, string url, string? title = null)
    {
        Label = label;
        Url = url;
        Title = title;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.ReferenceLinkDefinition;

    /// <summary>
    ///     链接标识
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    ///     链接地址
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    ///     链接标题
    /// </summary>
    public string? Title { get; init; }
}

/// <summary>
///     缩进代码块节点（4 空格缩进）
/// </summary>
public sealed record MarkdownIndentedCodeBlock : MarkdownNode
{
    public MarkdownIndentedCodeBlock(string content)
    {
        Content = content;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.IndentedCodeBlock;

    /// <summary>
    ///     代码内容
    /// </summary>
    public string Content { get; init; } = string.Empty;
}