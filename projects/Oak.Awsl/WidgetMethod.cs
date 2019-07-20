namespace Oak.Widget;

/// <summary>
///     Widget 函数声明
/// </summary>
public sealed class WidgetMethod
{
    /// <summary>
    ///     函数名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     参数列表（逗号分隔的原始字符串）
    /// </summary>
    public string Parameters { get; init; } = string.Empty;

    /// <summary>
    ///     函数体（原始字符串）
    /// </summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>
    ///     是否为微函数（编译到 WASM）
    /// </summary>
    public bool IsMicro { get; init; }
}
