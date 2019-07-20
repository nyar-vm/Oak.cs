namespace Oak.Svg;

/// <summary>
///     SVG 样式属性，包含填充、描边、透明度等视觉属性
/// </summary>
public sealed record SvgStyle
{
    /// <summary>
    ///     填充颜色（CSS 颜色值，如 "#FF0000"、"red"、"rgb(255,0,0)"、"none"）
    /// </summary>
    public string? Fill { get; init; }

    /// <summary>
    ///     填充透明度（0.0 ~ 1.0）
    /// </summary>
    public float FillOpacity { get; init; } = 1f;

    /// <summary>
    ///     填充规则（"nonzero" 或 "evenodd"）
    /// </summary>
    public string FillRule { get; init; } = "nonzero";

    /// <summary>
    ///     描边颜色
    /// </summary>
    public string? Stroke { get; init; }

    /// <summary>
    ///     描边宽度
    /// </summary>
    public float StrokeWidth { get; init; }

    /// <summary>
    ///     描边透明度（0.0 ~ 1.0）
    /// </summary>
    public float StrokeOpacity { get; init; } = 1f;

    /// <summary>
    ///     描边线帽（"butt"、"round"、"square"）
    /// </summary>
    public string StrokeLinecap { get; init; } = "butt";

    /// <summary>
    ///     描边连接（"miter"、"round"、"bevel"）
    /// </summary>
    public string StrokeLinejoin { get; init; } = "miter";

    /// <summary>
    ///     描边虚线偏移
    /// </summary>
    public float StrokeDashoffset { get; init; }

    /// <summary>
    ///     描边虚线模式（交替的线段长度和间隔长度）
    /// </summary>
    public float[] StrokeDasharray { get; init; } = [];

    /// <summary>
    ///     整体透明度（0.0 ~ 1.0）
    /// </summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>
    ///     是否无填充
    /// </summary>
    public bool IsFillNone => Fill is "none" or null;

    /// <summary>
    ///     是否无描边
    /// </summary>
    public bool IsStrokeNone => Stroke is "none" or null;
}
