namespace Oak.Widget;

/// <summary>
///     AWSL 属性种类
/// </summary>
public enum AwslAttributeKind
{
    /// <summary>
    ///     普通属性（class="foo", id="bar"）
    /// </summary>
    Normal,

    /// <summary>
    ///     事件绑定（@click="handler"）
    /// </summary>
    EventBinding,

    /// <summary>
    ///     响应式数据绑定（@bind="value"）
    /// </summary>
    DataBinding,

    /// <summary>
    ///     HTML 属性绑定（class=, style=, id= 等）
    /// </summary>
    PropertyBinding
}

/// <summary>
///     AWSL 元素属性，支持普通属性、事件绑定和数据绑定
/// </summary>
public sealed class AwslAttribute
{
    /// <summary>
    ///     属性名（不含 @ 前缀）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     属性值（原始字符串或表达式）
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    ///     属性种类
    /// </summary>
    public AwslAttributeKind Kind { get; init; }

    /// <summary>
    ///     是否为布尔属性（无值）
    /// </summary>
    public bool IsBooleanAttribute { get; init; }

    /// <summary>
    ///     转换为键值对（用于兼容现有 Dictionary&lt;string, string&gt; 接口）
    /// </summary>
    /// <returns>带有前缀的属性名和值</returns>
    public KeyValuePair<string, string> ToKeyValuePair()
    {
        var prefix = Kind switch
        {
            AwslAttributeKind.EventBinding => "@",
            AwslAttributeKind.DataBinding => "@",
            _ => string.Empty
        };

        return new KeyValuePair<string, string>(
            $"{prefix}{Name}",
            Value ?? (IsBooleanAttribute ? "true" : string.Empty));
    }
}
