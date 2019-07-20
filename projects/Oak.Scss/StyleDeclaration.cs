namespace Oak.Scss;

/// <summary>
///     样式声明
/// </summary>
public sealed class StyleDeclaration
{
    public StyleDeclaration(string property, string value, int specificity = 0, string? important = null)
    {
        Property = property;
        Value = value;
        Specificity = specificity;
        Important = important;
    }

    /// <summary>
    ///     CSS 属性名
    /// </summary>
    public string Property { get; }

    /// <summary>
    ///     CSS 属性值
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     是否 !important
    /// </summary>
    public string? Important { get; }

    /// <summary>
    ///     特异性权重
    /// </summary>
    public int Specificity { get; }
}