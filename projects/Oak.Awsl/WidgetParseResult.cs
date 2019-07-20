namespace Oak.Widget;

/// <summary>
///     Widget 组件解析结果
/// </summary>
public sealed class WidgetParseResult
{
    /// <summary>
    ///     组件名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     属性声明列表
    /// </summary>
    public IReadOnlyList<WidgetProperty> Properties { get; init; } = [];

    /// <summary>
    ///     函数声明列表
    /// </summary>
    public IReadOnlyList<WidgetMethod> Methods { get; init; } = [];

    /// <summary>
    ///     模板节点列表
    /// </summary>
    public IReadOnlyList<WidgetTemplateNode> TemplateNodes { get; init; } = [];

    /// <summary>
    ///     样式定义
    /// </summary>
    public IReadOnlyDictionary<string, string> Styles { get; init; } = new Dictionary<string, string>();
}
