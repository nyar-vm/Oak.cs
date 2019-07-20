using System.Text;
using Oak.DejaVu.Expressions;

namespace Oak.DejaVu.Ecosystem;

/// <summary>
///     组件注册表——可复用的模板组件（类似 ViewComponent / Partial with parameters）。
///     支持 slot 插槽和 props 参数传递。
/// </summary>
public sealed class ComponentRegistry
{
    private readonly Dictionary<string, ComponentDefinition> _components = new();

    /// <summary>
    ///     所有已注册组件
    /// </summary>
    public IReadOnlyDictionary<string, ComponentDefinition> Components => _components;

    /// <summary>
    ///     注册组件
    /// </summary>
    /// <param name="name">组件名</param>
    /// <param name="templateSource">组件模板源码</param>
    /// <param name="props">组件参数定义</param>
    /// <param name="slots">组件插槽定义</param>
    public void Register(string name, string templateSource, List<ComponentProp>? props = null, List<ComponentSlot>? slots = null)
    {
        var parser = new DejaVuParser("doki");
        var parseResult = parser.Parse(templateSource);

        _components[name] = new ComponentDefinition
        {
            Name = name,
            TemplateSource = templateSource,
            Nodes = parseResult.Nodes.ToList(),
            Props = props ?? [],
            Slots = slots ?? []
        };
    }

    /// <summary>
    ///     注册组件（从文件加载）
    /// </summary>
    public void RegisterFromFile(string name, string filePath, List<ComponentProp>? props = null, List<ComponentSlot>? slots = null)
    {
        if (!File.Exists(filePath)) return;

        var source = File.ReadAllText(filePath);
        Register(name, source, props, slots);
    }

    /// <summary>
    ///     检查组件是否已注册
    /// </summary>
    public bool HasComponent(string name)
    {
        return _components.ContainsKey(name);
    }

    /// <summary>
    ///     获取组件定义
    /// </summary>
    public ComponentDefinition? GetComponent(string name)
    {
        return _components.TryGetValue(name, out var component) ? component : null;
    }

    /// <summary>
    ///     渲染组件——将参数和插槽内容注入组件模板
    /// </summary>
    /// <param name="name">组件名</param>
    /// <param name="props">传入参数</param>
    /// <param name="slots">传入插槽内容</param>
    /// <returns>渲染后的节点列表</returns>
    public List<DejaVuTemplateNode> RenderComponent(string name, Dictionary<string, object> props, Dictionary<string, List<DejaVuTemplateNode>> slots)
    {
        if (!_components.TryGetValue(name, out var component))
        {
            return [new DejaVuTextNode { Text = $"<!-- 组件 \"{name}\" 未注册 -->" }];
        }

        var result = new List<DejaVuTemplateNode>();

        foreach (var node in component.Nodes)
        {
            result.AddRange(ExpandNode(node, props, slots, component));
        }

        return result;
    }

    private List<DejaVuTemplateNode> ExpandNode(DejaVuTemplateNode node, Dictionary<string, object> props, Dictionary<string, List<DejaVuTemplateNode>> slots, ComponentDefinition component)
    {
        switch (node)
        {
            case DejaVuCodeNode codeNode:
                var expandedCode = ExpandExpressions(codeNode.Code, props);
                return [new DejaVuCodeNode
                {
                    Code = expandedCode,
                    ParsedExpression = codeNode.ParsedExpression
                }];

            case DejaVuIfNode ifNode:
                var ifChildren = new List<DejaVuTemplateNode>();
                foreach (var child in ifNode.Children)
                {
                    ifChildren.AddRange(ExpandNode(child, props, slots, component));
                }

                var elseIfNodes = new List<DejaVuElseIfNode>();
                foreach (var elseIf in ifNode.ElseIfNodes)
                {
                    var elseIfChildren = new List<DejaVuTemplateNode>();
                    foreach (var child in elseIf.Children)
                    {
                        elseIfChildren.AddRange(ExpandNode(child, props, slots, component));
                    }

                    elseIfNodes.Add(new DejaVuElseIfNode
                    {
                        Condition = elseIf.Condition,
                        ParsedCondition = elseIf.ParsedCondition,
                        Children = elseIfChildren
                    });
                }

                var elseChildren = new List<DejaVuTemplateNode>();
                foreach (var child in ifNode.ElseChildren)
                {
                    elseChildren.AddRange(ExpandNode(child, props, slots, component));
                }

                return [new DejaVuIfNode
                {
                    Condition = ifNode.Condition,
                    ParsedCondition = ifNode.ParsedCondition,
                    Children = ifChildren,
                    ElseIfNodes = elseIfNodes,
                    ElseChildren = elseChildren
                }];

            case DejaVuLoopNode loopNode:
                var loopChildren = new List<DejaVuTemplateNode>();
                foreach (var child in loopNode.Children)
                {
                    loopChildren.AddRange(ExpandNode(child, props, slots, component));
                }

                return [new DejaVuLoopNode
                {
                    Expression = loopNode.Expression,
                    ParsedExpression = loopNode.ParsedExpression,
                    ItemName = loopNode.ItemName,
                    Children = loopChildren
                }];

            case DejaVuBlockNode blockNode:
                var slotName = blockNode.Name;
                if (slots.TryGetValue(slotName, out var slotContent))
                {
                    return slotContent;
                }

                var blockChildren = new List<DejaVuTemplateNode>();
                foreach (var child in blockNode.Children)
                {
                    blockChildren.AddRange(ExpandNode(child, props, slots, component));
                }

                return [new DejaVuBlockNode
                {
                    Name = blockNode.Name,
                    Children = blockChildren
                }];

            default:
                return [node];
        }
    }

    private static string ExpandExpressions(string code, Dictionary<string, object> props)
    {
        foreach (var (key, value) in props)
        {
            code = code.Replace($"{{{{{key}}}}}", value?.ToString() ?? "");
        }

        return code;
    }
}

/// <summary>
///     组件定义
/// </summary>
public sealed class ComponentDefinition
{
    /// <summary>
    ///     组件名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     组件模板源码
    /// </summary>
    public string TemplateSource { get; init; } = string.Empty;

    /// <summary>
    ///     解析后的节点列表
    /// </summary>
    public List<DejaVuTemplateNode> Nodes { get; init; } = [];

    /// <summary>
    ///     组件参数定义
    /// </summary>
    public List<ComponentProp> Props { get; init; } = [];

    /// <summary>
    ///     组件插槽定义
    /// </summary>
    public List<ComponentSlot> Slots { get; init; } = [];

    /// <summary>
    ///     生成组件调用签名
    /// </summary>
    public string Signature
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            sb.Append('(');
            sb.Append(string.Join(", ", Props.Select(p => $"{p.Name}: {p.Type}")));
            sb.Append(')');

            if (Slots.Count > 0)
            {
                sb.Append(" slots: [");
                sb.Append(string.Join(", ", Slots.Select(s => s.Name)));
                sb.Append(']');
            }

            return sb.ToString();
        }
    }
}

/// <summary>
///     组件参数定义
/// </summary>
public sealed class ComponentProp
{
    /// <summary>
    ///     参数名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     参数类型
    /// </summary>
    public string Type { get; init; } = "any";

    /// <summary>
    ///     参数默认值
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    ///     参数描述
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    ///     是否必填
    /// </summary>
    public bool Required { get; init; }
}

/// <summary>
///     组件插槽定义
/// </summary>
public sealed class ComponentSlot
{
    /// <summary>
    ///     插槽名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     插槽描述
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    ///     是否有默认内容
    /// </summary>
    public bool HasDefaultContent { get; init; }
}
