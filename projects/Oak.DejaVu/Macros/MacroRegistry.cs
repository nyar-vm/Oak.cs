namespace Oak.DejaVu.Macros;

/// <summary>
///     宏注册表
/// </summary>
public sealed class MacroRegistry
{
    private readonly Dictionary<string, MacroDefinition> _macros;

    public MacroRegistry()
    {
        _macros = new Dictionary<string, MacroDefinition>();
    }

    /// <summary>
    ///     注册宏
    /// </summary>
    public void Register(string name, MacroDefinition macro)
    {
        _macros[name] = macro;
    }

    /// <summary>
    ///     获取宏
    /// </summary>
    public MacroDefinition? Get(string name)
    {
        return _macros.TryGetValue(name, out var macro) ? macro : null;
    }

    /// <summary>
    ///     检查宏是否存在
    /// </summary>
    public bool Exists(string name)
    {
        return _macros.ContainsKey(name);
    }

    /// <summary>
    ///     展开宏
    /// </summary>
    public List<MacroNode> Expand(string name, Dictionary<string, object> arguments)
    {
        var macro = Get(name);
        if (macro == null) throw new KeyNotFoundException($"Macro not found: {name}");

        return macro.Expand(arguments);
    }
}

/// <summary>
///     宏节点接口
/// </summary>
public interface IMacroNode
{
    string Type { get; }
}

/// <summary>
///     宏文本节点
/// </summary>
public sealed class MacroTextNode : IMacroNode
{
    public string Text { get; init; } = string.Empty;
    public string Type => "text";
}

/// <summary>
///     宏代码节点
/// </summary>
public sealed class MacroCodeNode : IMacroNode
{
    public string Code { get; init; } = string.Empty;
    public string Type => "code";
}

/// <summary>
///     宏节点
/// </summary>
public sealed class MacroNode : IMacroNode
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object> Arguments { get; init; } = new();
    public string Type => "macro";
}

/// <summary>
///     宏定义
/// </summary>
public sealed class MacroDefinition
{
    /// <summary>
    ///     宏名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     参数列表
    /// </summary>
    public IReadOnlyList<string> Parameters { get; init; } = new List<string>();

    /// <summary>
    ///     宏体
    /// </summary>
    public IReadOnlyList<IMacroNode> Body { get; init; } = new List<IMacroNode>();

    /// <summary>
    ///     展开宏
    /// </summary>
    public List<MacroNode> Expand(Dictionary<string, object> arguments)
    {
        var result = new List<MacroNode>();
        foreach (var node in Body) result.AddRange(ExpandNode(node, arguments));
        return result;
    }

    /// <summary>
    ///     展开节点
    /// </summary>
    private List<MacroNode> ExpandNode(IMacroNode node, Dictionary<string, object> arguments)
    {
        return node switch
        {
            MacroTextNode textNode =>
                [new() { Name = "text", Arguments = new Dictionary<string, object> { ["text"] = textNode.Text } }],
            MacroCodeNode codeNode => ExpandCodeNode(codeNode, arguments),
            MacroNode macroNode => [macroNode],
            _ => []
        };
    }

    /// <summary>
    ///     展开代码节点
    /// </summary>
    private List<MacroNode> ExpandCodeNode(MacroCodeNode codeNode, Dictionary<string, object> arguments)
    {
        // 检查是否是参数引用
        var code = codeNode.Code.Trim();
        if (arguments.TryGetValue(code, out var value))
            return
            [
                new()
                {
                    Name = "text", Arguments = new Dictionary<string, object> { ["text"] = value?.ToString() ?? "" }
                }
            ];
        return [new() { Name = "code", Arguments = new Dictionary<string, object> { ["code"] = codeNode.Code } }];
    }
}