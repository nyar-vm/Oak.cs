namespace Oak.Scss;

/// <summary>
///     样式选择器类型
/// </summary>
public enum StyleSelectorType
{
    /// <summary>
    ///     元素类型选择器
    /// </summary>
    Type,

    /// <summary>
    ///     ID 选择器
    /// </summary>
    Id,

    /// <summary>
    ///     类选择器
    /// </summary>
    Class,

    /// <summary>
    ///     伪类选择器
    /// </summary>
    PseudoClass,

    /// <summary>
    ///     父引用选择器（&）
    /// </summary>
    ParentRef
}