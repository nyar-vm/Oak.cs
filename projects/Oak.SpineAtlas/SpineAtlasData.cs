namespace Oak.SpineAtlas;

/// <summary>
///     Spine Atlas 解析结果
/// </summary>
public sealed class SpineAtlasData
{
    /// <summary>
    ///     页面列表
    /// </summary>
    public List<SpineAtlasPage> Pages { get; init; } = [];

    /// <summary>
    ///     区域列表
    /// </summary>
    public List<SpineAtlasRegion> Regions { get; init; } = [];
}