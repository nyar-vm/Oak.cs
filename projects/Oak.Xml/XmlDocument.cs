namespace Oak.Xml;

/// <summary>
///     XML 文档
/// </summary>
public sealed class XmlDocument
{
    /// <summary>
    ///     XML 声明
    /// </summary>
    public XmlDeclaration? Declaration { get; init; }

    /// <summary>
    ///     根元素
    /// </summary>
    public XmlElement? Root { get; init; }
}