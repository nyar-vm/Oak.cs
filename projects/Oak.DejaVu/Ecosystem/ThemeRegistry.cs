using System.Text;

namespace Oak.DejaVu.Ecosystem;

/// <summary>
///     主题注册表——模板主题包（CSS 变量 + 组件覆盖 + 布局覆盖）。
///     支持主题继承和变量覆盖。
/// </summary>
public sealed class ThemeRegistry
{
    private readonly Dictionary<string, ThemeDefinition> _themes = new();

    /// <summary>
    ///     所有已注册主题
    /// </summary>
    public IReadOnlyDictionary<string, ThemeDefinition> Themes => _themes;

    /// <summary>
    ///     当前激活的主题名
    /// </summary>
    public string? ActiveThemeName { get; private set; }

    /// <summary>
    ///     注册主题
    /// </summary>
    /// <param name="name">主题名</param>
    /// <param name="baseThemeName">基础主题名（主题继承）</param>
    /// <param name="cssVariables">CSS 变量覆盖</param>
    /// <param name="componentOverrides">组件覆盖</param>
    /// <param name="layoutOverrides">布局覆盖</param>
    public void Register(string name, string? baseThemeName = null, Dictionary<string, string>? cssVariables = null, Dictionary<string, string>? componentOverrides = null, Dictionary<string, string>? layoutOverrides = null)
    {
        var resolvedVariables = new Dictionary<string, string>();
        var resolvedComponents = new Dictionary<string, string>();
        var resolvedLayouts = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(baseThemeName) && _themes.TryGetValue(baseThemeName, out var baseTheme))
        {
            foreach (var (key, value) in baseTheme.CssVariables)
            {
                resolvedVariables[key] = value;
            }

            foreach (var (key, value) in baseTheme.ComponentOverrides)
            {
                resolvedComponents[key] = value;
            }

            foreach (var (key, value) in baseTheme.LayoutOverrides)
            {
                resolvedLayouts[key] = value;
            }
        }

        if (cssVariables != null)
        {
            foreach (var (key, value) in cssVariables)
            {
                resolvedVariables[key] = value;
            }
        }

        if (componentOverrides != null)
        {
            foreach (var (key, value) in componentOverrides)
            {
                resolvedComponents[key] = value;
            }
        }

        if (layoutOverrides != null)
        {
            foreach (var (key, value) in layoutOverrides)
            {
                resolvedLayouts[key] = value;
            }
        }

        _themes[name] = new ThemeDefinition
        {
            Name = name,
            BaseThemeName = baseThemeName,
            CssVariables = resolvedVariables,
            ComponentOverrides = resolvedComponents,
            LayoutOverrides = resolvedLayouts
        };
    }

    /// <summary>
    ///     激活主题
    /// </summary>
    public void Activate(string name)
    {
        if (!_themes.ContainsKey(name))
        {
            throw new InvalidOperationException($"主题 \"{name}\" 未注册");
        }

        ActiveThemeName = name;
    }

    /// <summary>
    ///     获取当前激活的主题
    /// </summary>
    public ThemeDefinition? ActiveTheme =>
        ActiveThemeName != null && _themes.TryGetValue(ActiveThemeName, out var theme) ? theme : null;

    /// <summary>
    ///     获取主题
    /// </summary>
    public ThemeDefinition? GetTheme(string name)
    {
        return _themes.TryGetValue(name, out var theme) ? theme : null;
    }

    /// <summary>
    ///     生成当前主题的 CSS 变量声明
    /// </summary>
    /// <param name="selector">CSS 选择器，默认 :root</param>
    /// <returns>CSS 源码</returns>
    public string GenerateCssVariables(string selector = ":root")
    {
        var theme = ActiveTheme;
        if (theme == null || theme.CssVariables.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{selector} {{");

        foreach (var (key, value) in theme.CssVariables.OrderBy(kv => kv.Key))
        {
            sb.AppendLine($"    --{key}: {value};");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    ///     获取组件覆盖模板
    /// </summary>
    /// <param name="componentName">组件名</param>
    /// <returns>覆盖模板源码，或 null</returns>
    public string? GetComponentOverride(string componentName)
    {
        return ActiveTheme?.ComponentOverrides.TryGetValue(componentName, out var source) == true
            ? source
            : null;
    }

    /// <summary>
    ///     获取布局覆盖模板
    /// </summary>
    /// <param name="layoutName">布局名</param>
    /// <returns>覆盖模板源码，或 null</returns>
    public string? GetLayoutOverride(string layoutName)
    {
        return ActiveTheme?.LayoutOverrides.TryGetValue(layoutName, out var source) == true
            ? source
            : null;
    }

    /// <summary>
    ///     解析 CSS 变量引用——将 var(--xxx) 替换为实际值
    /// </summary>
    /// <param name="input">包含 var() 引用的字符串</param>
    /// <returns>替换后的字符串</returns>
    public string ResolveCssVariables(string input)
    {
        var theme = ActiveTheme;
        if (theme == null) return input;

        foreach (var (key, value) in theme.CssVariables)
        {
            input = input.Replace($"var(--{key})", value);
        }

        return input;
    }
}

/// <summary>
///     主题定义
/// </summary>
public sealed class ThemeDefinition
{
    /// <summary>
    ///     主题名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     基础主题名
    /// </summary>
    public string? BaseThemeName { get; init; }

    /// <summary>
    ///     CSS 变量（已合并基础主题）
    /// </summary>
    public Dictionary<string, string> CssVariables { get; init; } = new();

    /// <summary>
    ///     组件覆盖（组件名 → 覆盖模板源码）
    /// </summary>
    public Dictionary<string, string> ComponentOverrides { get; init; } = new();

    /// <summary>
    ///     布局覆盖（布局名 → 覆盖模板源码）
    /// </summary>
    public Dictionary<string, string> LayoutOverrides { get; init; } = new();

    /// <summary>
    ///     继承深度
    /// </summary>
    public int InheritanceDepth
    {
        get
        {
            var depth = 0;
            var current = BaseThemeName;
            while (current != null)
            {
                depth++;
                current = null;
            }

            return depth;
        }
    }
}
