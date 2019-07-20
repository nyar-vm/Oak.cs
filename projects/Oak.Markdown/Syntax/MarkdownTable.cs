namespace Oak.Markdown.Syntax;

/// <summary>
///     表格节点
/// </summary>
public sealed record MarkdownTable : MarkdownNode
{
    public MarkdownTable(MarkdownTableRow header, IReadOnlyList<MarkdownTableRow> rows)
    {
        Header = header;
        Rows = rows;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.Table;

    /// <summary>
    ///     表头行
    /// </summary>
    public MarkdownTableRow Header { get; init; }

    /// <summary>
    ///     数据行
    /// </summary>
    public IReadOnlyList<MarkdownTableRow> Rows { get; init; } = [];
}

/// <summary>
///     表格行节点
/// </summary>
public sealed record MarkdownTableRow : MarkdownNode
{
    public MarkdownTableRow(IReadOnlyList<MarkdownTableCell> cells)
    {
        Cells = cells;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.TableRow;

    /// <summary>
    ///     单元格
    /// </summary>
    public IReadOnlyList<MarkdownTableCell> Cells { get; init; } = [];
}

/// <summary>
///     表格单元格节点
/// </summary>
public sealed record MarkdownTableCell : MarkdownNode
{
    public MarkdownTableCell(IReadOnlyList<MarkdownNode> children)
    {
        Children = children;
    }

    public override MarkdownNodeType NodeType => MarkdownNodeType.TableCell;

    /// <summary>
    ///     单元格内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> Children { get; init; } = [];
}