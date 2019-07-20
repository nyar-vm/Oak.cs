using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Neural;

/// <summary>
///     配置项键值对，用于 Neural 训练/推理等配置块中的自定义条目
/// </summary>
/// <para>示例：</para>
/// <code>
/// inference {
///     device = "gpu";
///     precision = "fp16";
/// }
/// </code>
public sealed record ConfigEntry
{
    /// <summary>
    ///     配置键名
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    ///     配置值（字面量表达式）
    /// </summary>
    public TermAtomicLiteral Value { get; init; } = new();
}
