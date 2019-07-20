namespace Oak.SpineAtlas;

/// <summary>
///     Spine Atlas 区域定义
/// </summary>
public sealed record SpineAtlasRegion
{
    /// <summary>
    ///     区域名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     所属页面索引
    /// </summary>
    public int PageIndex { get; init; }

    /// <summary>
    ///     X 坐标
    /// </summary>
    public int X { get; init; }

    /// <summary>
    ///     Y 坐标
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    ///     宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     X 偏移量
    /// </summary>
    public int OffsetX { get; init; }

    /// <summary>
    ///     Y 偏移量
    /// </summary>
    public int OffsetY { get; init; }

    /// <summary>
    ///     原始宽度
    /// </summary>
    public int OriginalWidth { get; init; }

    /// <summary>
    ///     原始高度
    /// </summary>
    public int OriginalHeight { get; init; }

    /// <summary>
    ///     是否旋转 90 度
    /// </summary>
    public bool IsRotated { get; init; }

    /// <summary>
    ///     是否分割（九宫格）
    /// </summary>
    public bool IsSplit { get; init; }

    /// <summary>
    ///     分割数据 [left, right, top, bottom]
    /// </summary>
    public int[]? Splits { get; init; }

    /// <summary>
    ///     填充数据 [left, right, top, bottom]
    /// </summary>
    public int[]? Pads { get; init; }
}