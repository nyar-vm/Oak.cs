namespace Oak.Syntax;

/// <summary>
///     绿树叶子节点，表示词法单元
/// </summary>
public sealed class GreenLeafNode : GreenNode
{
    /// <summary>
    ///     创建叶子节点
    /// </summary>
    public GreenLeafNode(NodeKind kind, int width, string? text = null)
    {
        Kind = kind;
        Width = width;
        Text = text;
    }

    /// <inheritdoc />
    public override NodeKind Kind { get; }

    /// <inheritdoc />
    public override int Width { get; }

    /// <inheritdoc />
    public override int ChildCount => 0;

    /// <summary>
    ///     词法单元的文本内容
    /// </summary>
    public string? Text { get; }

    /// <inheritdoc />
    public override GreenNode? GetChild(int index)
    {
        return null;
    }

    /// <summary>
    ///     叶子节点直接写入文本
    /// </summary>
    public override void WriteTo(TextWriter writer)
    {
        if (Text is not null)
        {
            writer.Write(Text);
        }
    }
}
