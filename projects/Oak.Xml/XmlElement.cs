namespace Oak.Xml;

/// <summary>
///     XML 元素
/// </summary>
public sealed class XmlElement
{
    /// <summary>
    ///     元素名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<XmlAttribute> Attributes { get; init; } = [];

    /// <summary>
    ///     子元素列表
    /// </summary>
    public List<XmlElement> Children { get; init; } = [];

    /// <summary>
    ///     文本内容
    /// </summary>
    public string? TextContent { get; init; }

    /// <summary>
    ///     根据属性名查找属性值
    /// </summary>
    public string? GetAttribute(string name)
    {
        foreach (var attr in Attributes)
            if (attr.Name == name)
                return attr.Value;

        return null;
    }

    /// <summary>
    ///     根据元素名查找子元素
    /// </summary>
    public IEnumerable<XmlElement> GetElements(string name)
    {
        foreach (var child in Children)
            if (child.Name == name)
                yield return child;
    }
}