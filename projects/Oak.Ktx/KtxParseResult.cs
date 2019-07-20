namespace Oak.Ktx;

/// <summary>
///     KTX 解析结果
/// </summary>
public sealed class KtxParseResult
{
    /// <summary>
    ///     纹理宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     纹理高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     纹理深度
    /// </summary>
    public int Depth { get; init; } = 1;

    /// <summary>
    ///     MipMap 层级数
    /// </summary>
    public int MipLevels { get; init; } = 1;

    /// <summary>
    ///     数组层数
    /// </summary>
    public int ArrayLayers { get; init; } = 1;

    /// <summary>
    ///     纹理格式
    /// </summary>
    public TextureFormat Format { get; init; }

    /// <summary>
    ///     纹理维度
    /// </summary>
    public TextureDimension Dimension { get; init; }

    /// <summary>
    ///     原始纹理数据（第一层第一面）
    /// </summary>
    public byte[] RawData { get; init; } = [];

    /// <summary>
    ///     所有 Mip 层级数据
    /// </summary>
    public List<byte[]> MipData { get; init; } = [];

    /// <summary>
    ///     OpenGL 内部格式（KTX1）
    /// </summary>
    public uint GlInternalFormat { get; init; }

    /// <summary>
    ///     Vulkan 格式（KTX2）
    /// </summary>
    public uint VkFormat { get; init; }
}