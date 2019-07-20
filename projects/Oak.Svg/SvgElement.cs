namespace Oak.Svg;

/// <summary>
///     SVG 元素类型
/// </summary>
public enum SvgElementType : byte
{
    /// <summary>
    ///     SVG 根元素
    /// </summary>
    Svg = 0,

    /// <summary>
    ///     分组元素
    /// </summary>
    Group = 1,

    /// <summary>
    ///     路径元素
    /// </summary>
    Path = 2,

    /// <summary>
    ///     矩形元素
    /// </summary>
    Rect = 3,

    /// <summary>
    ///     圆形元素
    /// </summary>
    Circle = 4,

    /// <summary>
    ///     椭圆元素
    /// </summary>
    Ellipse = 5,

    /// <summary>
    ///     线段元素
    /// </summary>
    Line = 6,

    /// <summary>
    ///     折线元素
    /// </summary>
    Polyline = 7,

    /// <summary>
    ///     多边形元素
    /// </summary>
    Polygon = 8,

    /// <summary>
    ///     文本元素
    /// </summary>
    Text = 9,

    /// <summary>
    ///     定义元素
    /// </summary>
    Defs = 10,

    /// <summary>
    ///     引用元素
    /// </summary>
    Use = 11,

    /// <summary>
    ///     图像元素
    /// </summary>
    Image = 12,

    /// <summary>
    ///     线性渐变
    /// </summary>
    LinearGradient = 13,

    /// <summary>
    ///     径向渐变
    /// </summary>
    RadialGradient = 14,

    /// <summary>
    ///     裁剪路径
    /// </summary>
    ClipPath = 15,

    /// <summary>
    ///     遮罩
    /// </summary>
    Mask = 16,

    /// <summary>
    ///     未知元素
    /// </summary>
    Unknown = 255
}

/// <summary>
///     SVG 元素基类
/// </summary>
public abstract class SvgElement
{
    /// <summary>
    ///     元素类型
    /// </summary>
    public abstract SvgElementType ElementType { get; }

    /// <summary>
    ///     元素 ID
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     CSS 类名
    /// </summary>
    public string Class { get; init; } = string.Empty;

    /// <summary>
    ///     变换列表
    /// </summary>
    public List<SvgTransform> Transforms { get; init; } = [];

    /// <summary>
    ///     样式属性
    /// </summary>
    public SvgStyle Style { get; init; } = new();

    /// <summary>
    ///     子元素列表
    /// </summary>
    public List<SvgElement> Children { get; init; } = [];

    /// <summary>
    ///     是否为容器元素（可包含子元素）
    /// </summary>
    public virtual bool IsContainer => false;
}
