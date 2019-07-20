namespace Oak.Markdown.Syntax;

/// <summary>
///     水平分割线节点
/// </summary>
public sealed record MarkdownHorizontalRule : MarkdownNode
{
    public override MarkdownNodeType NodeType => MarkdownNodeType.HorizontalRule;
}