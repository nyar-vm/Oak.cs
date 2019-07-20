namespace Oak.Widget;

/// <summary>
///     Widget 属性声明
/// </summary>
public sealed class WidgetProperty
{
    /// <summary>
    ///     属性名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     类型名
    /// </summary>
    public string TypeName { get; init; } = "auto";

    /// <summary>
    ///     是否只读
    /// </summary>
    public bool IsReadonly { get; init; }

    /// <summary>
    ///     默认值（原始字符串）
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    ///     默认值种类
    /// </summary>
    public WidgetValueKind DefaultValueKind { get; init; }
}

/// <summary>
///     Widget 值种类
/// </summary>
public enum WidgetValueKind
{
    None,
    Boolean,
    Number,
    String,
    Identifier,
    Array,
    Object,
    Expression
}
