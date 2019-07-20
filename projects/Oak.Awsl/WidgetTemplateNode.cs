namespace Oak.Widget;

/// <summary>
///     Widget 模板节点基类
/// </summary>
public abstract class WidgetTemplateNode;

/// <summary>
///     文本节点
/// </summary>
public sealed class WidgetTextNode : WidgetTemplateNode
{
    public string Text { get; init; } = string.Empty;
}

/// <summary>
///     JSX 型插值节点，表示模板中的 {expression}
/// </summary>
public sealed class WidgetInterpolationNode : WidgetTemplateNode
{
    /// <summary>
    ///     插值表达式（花括号内的内容）
    /// </summary>
    public string Expression { get; init; } = string.Empty;
}

/// <summary>
///     元素节点
/// </summary>
public sealed class WidgetElementNode : WidgetTemplateNode
{
    public string TagName { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<WidgetTemplateNode> Children { get; init; } = [];

    public bool IsSelfClosing { get; init; }
}

/// <summary>
///     条件节点
/// </summary>
public sealed class WidgetIfNode : WidgetTemplateNode
{
    public string Condition { get; init; } = "true";

    public IReadOnlyList<WidgetTemplateNode> Children { get; init; } = [];

    public IReadOnlyList<WidgetTemplateNode> ElseChildren { get; init; } = [];
}

/// <summary>
///     循环节点
/// </summary>
public sealed class WidgetForNode : WidgetTemplateNode
{
    public string Iterator { get; init; } = "item";

    public string Iterable { get; init; } = "items";

    public IReadOnlyList<WidgetTemplateNode> Children { get; init; } = [];
}
