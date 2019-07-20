namespace Oak.Ini;

/// <summary>
///     INI 语言配置。
/// </summary>
public sealed class IniLanguage : Oak.Syntax.Language
{
    /// <summary>
    ///     语言名称。
    /// </summary>
    public override string Name => "INI";

    /// <summary>
    ///     注释分隔符（默认 ";"）。
    /// </summary>
    public string CommentDelimiter { get; init; } = ";";

    /// <summary>
    ///     是否允许 # 作为注释。
    /// </summary>
    public bool AllowHashComments { get; init; } = true;

    /// <summary>
    ///     是否允许多行值。
    /// </summary>
    public bool AllowMultilineValues { get; init; }

    /// <summary>
    ///     是否允许无节键值对。
    /// </summary>
    public bool AllowGlobalKeys { get; init; } = true;
}
