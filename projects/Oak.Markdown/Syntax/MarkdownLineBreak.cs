namespace Oak.Markdown.Syntax;

/// <summary>
///     硬换行节点
/// </summary>
public sealed record MarkdownLineBreak : MarkdownNode
{
    public override MarkdownNodeType NodeType => MarkdownNodeType.LineBreak;
}