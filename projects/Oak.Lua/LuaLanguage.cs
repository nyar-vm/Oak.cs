namespace Oak.Lua;

/// <summary>
///     Lua 语言配置。
/// </summary>
public sealed class LuaLanguage : Oak.Syntax.Language
{
    /// <summary>
    ///     语言名称。
    /// </summary>
    public override string Name => "Lua";

    /// <summary>
    ///     是否启用 Lua 5.1 兼容模式。
    /// </summary>
    public bool Lua51Compat { get; init; }

    /// <summary>
    ///     是否启用整数字面量（Lua 5.3+）。
    /// </summary>
    public bool IntegerLiterals { get; init; } = true;

    /// <summary>
    ///     是否启用位运算符（Lua 5.3+）。
    /// </summary>
    public bool BitwiseOperators { get; init; } = true;

    /// <summary>
    ///     是否启用 goto 语句。
    /// </summary>
    public bool GotoStatement { get; init; } = true;
}
