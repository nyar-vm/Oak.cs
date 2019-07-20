namespace Oak.DejaVu;

/// <summary>
///     DejaVu 模板解析结果
/// </summary>
public sealed class DejaVuParseResult
{
    /// <summary>
    ///     模板节点列表
    /// </summary>
    public IReadOnlyList<DejaVuTemplateNode> Nodes { get; init; } = [];

    /// <summary>
    ///     模板类型（dora 或 doki）
    /// </summary>
    public string TemplateType { get; init; } = string.Empty;
}