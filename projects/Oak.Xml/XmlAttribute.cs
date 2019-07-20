namespace Oak.Xml;

/// <summary>
///     XML 属性
/// </summary>
public sealed class XmlAttribute
{
    /// <summary>
    ///     属性名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     属性值
    /// </summary>
    public string Value { get; init; } = string.Empty;
}