namespace Oak.SpineAtlas;

/// <summary>
///     Spine Atlas 页面定义
/// </summary>
public sealed class SpineAtlasPage
{
    /// <summary>
    ///     纹理文件路径
    /// </summary>
    public string TextureFilePath { get; init; } = string.Empty;

    /// <summary>
    ///     纹理宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     纹理高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     纹理格式
    /// </summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>
    ///     最小化过滤方式
    /// </summary>
    public string FilterMin { get; init; } = string.Empty;

    /// <summary>
    ///     放大过滤方式
    /// </summary>
    public string FilterMag { get; init; } = string.Empty;

    /// <summary>
    ///     S 轴环绕方式
    /// </summary>
    public string WrapS { get; init; } = "clampToEdge";

    /// <summary>
    ///     T 轴环绕方式
    /// </summary>
    public string WrapT { get; init; } = "clampToEdge";
}