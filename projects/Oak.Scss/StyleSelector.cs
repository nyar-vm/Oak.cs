namespace Oak.Scss;

/// <summary>
///     样式选择器
/// </summary>
public readonly struct StyleSelector : IEquatable<StyleSelector>
{
    /// <summary>
    ///     选择器类型
    /// </summary>
    public readonly StyleSelectorType Type;

    /// <summary>
    ///     选择器值
    /// </summary>
    public readonly string Value;

    public StyleSelector(StyleSelectorType type, string value)
    {
        Type = type;
        Value = value;
    }

    /// <inheritdoc />
    public bool Equals(StyleSelector other)
    {
        return Type == other.Type && Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is StyleSelector other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Value);
    }

    /// <summary>
    ///     相等运算符
    /// </summary>
    public static bool operator ==(StyleSelector left, StyleSelector right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     不等运算符
    /// </summary>
    public static bool operator !=(StyleSelector left, StyleSelector right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     创建类型选择器
    /// </summary>
    public static StyleSelector ByType(string typeName)
    {
        return new StyleSelector(StyleSelectorType.Type, typeName);
    }

    /// <summary>
    ///     创建 ID 选择器
    /// </summary>
    public static StyleSelector ById(string id)
    {
        return new StyleSelector(StyleSelectorType.Id, id);
    }

    /// <summary>
    ///     创建类选择器
    /// </summary>
    public static StyleSelector ByClass(string className)
    {
        return new StyleSelector(StyleSelectorType.Class, className);
    }

    /// <summary>
    ///     创建伪类选择器
    /// </summary>
    public static StyleSelector ByPseudoClass(string name)
    {
        return new StyleSelector(StyleSelectorType.PseudoClass, name);
    }

    /// <summary>
    ///     创建父引用选择器
    /// </summary>
    public static StyleSelector ParentRef(string suffix = "")
    {
        return new StyleSelector(StyleSelectorType.ParentRef, "&" + suffix);
    }
}