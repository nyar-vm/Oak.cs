namespace Oak.Markdown.Syntax;

/// <summary>
///     图片节点
/// </summary>
public sealed record MarkdownImage : MarkdownNode
{
    public MarkdownImage(string url, string alt, string? title = null)
    {
        Url = url;
        Alt = alt;
        Title = title;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Image;

    /// <summary>
    ///     图片地址
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    ///     替代文本
    /// </summary>
    public string Alt { get; init; } = string.Empty;

    /// <summary>
    ///     图片标题
    /// </summary>
    public string? Title { get; init; }
}