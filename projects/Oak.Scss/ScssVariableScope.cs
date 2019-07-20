namespace Oak.Scss;

/// <summary>
///     SCSS 变量作用域
/// </summary>
public sealed class ScssVariableScope
{
    private readonly Dictionary<string, string> _variables = new();

    public ScssVariableScope(ScssVariableScope? parent = null)
    {
        Parent = parent;
    }

    /// <summary>
    ///     父作用域
    /// </summary>
    public ScssVariableScope? Parent { get; }

    /// <summary>
    ///     获取所有变量（包含父作用域）
    /// </summary>
    public IReadOnlyDictionary<string, string> AllVariables
    {
        get
        {
            var result = new Dictionary<string, string>();
            CollectInto(result);
            return result;
        }
    }

    /// <summary>
    ///     定义变量
    /// </summary>
    public void Define(string name, string value)
    {
        _variables[name] = value;
    }

    /// <summary>
    ///     尝试解析变量
    /// </summary>
    public bool TryResolve(string name, out string value)
    {
        if (_variables.TryGetValue(name, out value!)) return true;

        if (Parent != null) return Parent.TryResolve(name, out value!);

        value = default!;
        return false;
    }

    /// <summary>
    ///     解析变量
    /// </summary>
    public string? Resolve(string name)
    {
        return TryResolve(name, out var value) ? value : null;
    }

    /// <summary>
    ///     创建子作用域
    /// </summary>
    public ScssVariableScope Push()
    {
        return new ScssVariableScope(this);
    }

    private void CollectInto(Dictionary<string, string> target)
    {
        Parent?.CollectInto(target);

        foreach (var (key, value) in _variables) target[key] = value;
    }
}