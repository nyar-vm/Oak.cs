namespace Oak.Toml;

/// <summary>
///     TOML 语言配置。
/// </summary>
public sealed class TomlLanguage : Oak.Syntax.Language
{
    /// <summary>
    ///     语言名称。
    /// </summary>
    public override string Name => "TOML";

    /// <summary>
    ///     是否允许内联表。
    /// </summary>
    public bool AllowInlineTables { get; init; } = true;

    /// <summary>
    ///     是否允许多行字符串。
    /// </summary>
    public bool AllowMultilineStrings { get; init; } = true;

    /// <summary>
    ///     是否允许表达式。
    /// </summary>
    public bool AllowExpressions { get; init; }

    /// <summary>
    ///     是否允许日期时间字面量。
    /// </summary>
    public bool AllowDateTime { get; init; } = true;
}
