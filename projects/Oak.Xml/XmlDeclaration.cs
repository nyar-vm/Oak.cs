namespace Oak.Xml;

/// <summary>
///     XML 声明
/// </summary>
public sealed class XmlDeclaration
{
    /// <summary>
    ///     XML 版本
    /// </summary>
    public string Version { get; init; } = "1.0";

    /// <summary>
    ///     字符编码
    /// </summary>
    public string Encoding { get; init; } = "UTF-8";
}