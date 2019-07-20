namespace Oak.Svg;

/// <summary>
///     SVG 根元素
/// </summary>
public sealed class SvgRootElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Svg;

    public override bool IsContainer => true;

    /// <summary>
    ///     视图框（minX, minY, width, height）
    /// </summary>
    public float[] ViewBox { get; init; } = [];

    /// <summary>
    ///     文档宽度
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    ///     文档高度
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    ///     宽度单位
    /// </summary>
    public string WidthUnit { get; init; } = string.Empty;

    /// <summary>
    ///     高度单位
    /// </summary>
    public string HeightUnit { get; init; } = string.Empty;
}

/// <summary>
///     SVG 分组元素（g）
/// </summary>
public sealed class SvgGroupElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Group;

    public override bool IsContainer => true;
}

/// <summary>
///     SVG 路径元素（path）
/// </summary>
public sealed class SvgPathElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Path;

    /// <summary>
    ///     路径数据命令列表
    /// </summary>
    public List<SvgPathCommand> Commands { get; init; } = [];
}

/// <summary>
///     SVG 矩形元素（rect）
/// </summary>
public sealed class SvgRectElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Rect;

    /// <summary>
    ///     左上角 X 坐标
    /// </summary>
    public float X { get; init; }

    /// <summary>
    ///     左上角 Y 坐标
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    ///     宽度
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    ///     高度
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    ///     水平圆角半径
    /// </summary>
    public float Rx { get; init; }

    /// <summary>
    ///     垂直圆角半径
    /// </summary>
    public float Ry { get; init; }

    /// <summary>
    ///     是否有圆角
    /// </summary>
    public bool HasRoundedCorners => Rx > 0 || Ry > 0;
}

/// <summary>
///     SVG 圆形元素（circle）
/// </summary>
public sealed class SvgCircleElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Circle;

    /// <summary>
    ///     圆心 X 坐标
    /// </summary>
    public float Cx { get; init; }

    /// <summary>
    ///     圆心 Y 坐标
    /// </summary>
    public float Cy { get; init; }

    /// <summary>
    ///     半径
    /// </summary>
    public float R { get; init; }
}

/// <summary>
///     SVG 椭圆元素（ellipse）
/// </summary>
public sealed class SvgEllipseElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Ellipse;

    /// <summary>
    ///     圆心 X 坐标
    /// </summary>
    public float Cx { get; init; }

    /// <summary>
    ///     圆心 Y 坐标
    /// </summary>
    public float Cy { get; init; }

    /// <summary>
    ///     水平半径
    /// </summary>
    public float Rx { get; init; }

    /// <summary>
    ///     垂直半径
    /// </summary>
    public float Ry { get; init; }
}

/// <summary>
///     SVG 线段元素（line）
/// </summary>
public sealed class SvgLineElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Line;

    /// <summary>
    ///     起点 X 坐标
    /// </summary>
    public float X1 { get; init; }

    /// <summary>
    ///     起点 Y 坐标
    /// </summary>
    public float Y1 { get; init; }

    /// <summary>
    ///     终点 X 坐标
    /// </summary>
    public float X2 { get; init; }

    /// <summary>
    ///     终点 Y 坐标
    /// </summary>
    public float Y2 { get; init; }
}

/// <summary>
///     SVG 折线元素（polyline）
/// </summary>
public sealed class SvgPolylineElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Polyline;

    /// <summary>
    ///     顶点坐标列表（x0, y0, x1, y1, ...）
    /// </summary>
    public float[] Points { get; init; } = [];
}

/// <summary>
///     SVG 多边形元素（polygon）
/// </summary>
public sealed class SvgPolygonElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Polygon;

    /// <summary>
    ///     顶点坐标列表（x0, y0, x1, y1, ...）
    /// </summary>
    public float[] Points { get; init; } = [];
}

/// <summary>
///     SVG 文本元素（text）
/// </summary>
public sealed class SvgTextElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Text;

    /// <summary>
    ///     文本基线 X 坐标
    /// </summary>
    public float X { get; init; }

    /// <summary>
    ///     文本基线 Y 坐标
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    ///     字体族
    /// </summary>
    public string FontFamily { get; init; } = string.Empty;

    /// <summary>
    ///     字体大小
    /// </summary>
    public float FontSize { get; init; } = 16f;

    /// <summary>
    ///     文本锚点（"start"、"middle"、"end"）
    /// </summary>
    public string TextAnchor { get; init; } = "start";
}

/// <summary>
///     SVG 定义元素（defs）
/// </summary>
public sealed class SvgDefsElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Defs;

    public override bool IsContainer => true;
}

/// <summary>
///     SVG 引用元素（use）
/// </summary>
public sealed class SvgUseElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Use;

    /// <summary>
    ///     引用目标（href 或 xlink:href）
    /// </summary>
    public string Href { get; init; } = string.Empty;

    /// <summary>
    ///     X 偏移
    /// </summary>
    public float X { get; init; }

    /// <summary>
    ///     Y 偏移
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    ///     宽度
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    ///     高度
    /// </summary>
    public float Height { get; init; }
}

/// <summary>
///     SVG 图像元素（image）
/// </summary>
public sealed class SvgImageElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Image;

    /// <summary>
    ///     图像源地址
    /// </summary>
    public string Href { get; init; } = string.Empty;

    /// <summary>
    ///     左上角 X 坐标
    /// </summary>
    public float X { get; init; }

    /// <summary>
    ///     左上角 Y 坐标
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    ///     宽度
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    ///     高度
    /// </summary>
    public float Height { get; init; }
}

/// <summary>
///     SVG 线性渐变元素（linearGradient）
/// </summary>
public sealed class SvgLinearGradientElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.LinearGradient;

    public override bool IsContainer => true;

    /// <summary>
    ///     渐变起点 X（0~1 或绝对值）
    /// </summary>
    public float X1 { get; init; }

    /// <summary>
    ///     渐变起点 Y
    /// </summary>
    public float Y1 { get; init; }

    /// <summary>
    ///     渐变终点 X
    /// </summary>
    public float X2 { get; init; } = 1f;

    /// <summary>
    ///     渐变终点 Y
    /// </summary>
    public float Y2 { get; init; }

    /// <summary>
    ///     渐变单位（"userSpaceOnUse" 或 "objectBoundingBox"）
    /// </summary>
    public string GradientUnits { get; init; } = "objectBoundingBox";
}

/// <summary>
///     SVG 径向渐变元素（radialGradient）
/// </summary>
public sealed class SvgRadialGradientElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.RadialGradient;

    public override bool IsContainer => true;

    /// <summary>
    ///     圆心 X
    /// </summary>
    public float Cx { get; init; } = 0.5f;

    /// <summary>
    ///     圆心 Y
    /// </summary>
    public float Cy { get; init; } = 0.5f;

    /// <summary>
    ///     半径
    /// </summary>
    public float R { get; init; } = 0.5f;

    /// <summary>
    ///     焦点 X
    /// </summary>
    public float Fx { get; init; }

    /// <summary>
    ///     焦点 Y
    /// </summary>
    public float Fy { get; init; }

    /// <summary>
    ///     渐变单位
    /// </summary>
    public string GradientUnits { get; init; } = "objectBoundingBox";
}

/// <summary>
///     SVG 裁剪路径元素（clipPath）
/// </summary>
public sealed class SvgClipPathElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.ClipPath;

    public override bool IsContainer => true;

    /// <summary>
    ///     裁剪单位
    /// </summary>
    public string ClipPathUnits { get; init; } = "userSpaceOnUse";
}

/// <summary>
///     SVG 遮罩元素（mask）
/// </summary>
public sealed class SvgMaskElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Mask;

    public override bool IsContainer => true;

    /// <summary>
    ///     遮罩单位
    /// </summary>
    public string MaskUnits { get; init; } = "objectBoundingBox";

    /// <summary>
    ///     遮罩内容单位
    /// </summary>
    public string MaskContentUnits { get; init; } = "userSpaceOnUse";
}

/// <summary>
///     SVG 未知元素（用于保留不支持的元素）
/// </summary>
public sealed class SvgUnknownElement : SvgElement
{
    public override SvgElementType ElementType => SvgElementType.Unknown;

    /// <summary>
    ///     原始元素名称
    /// </summary>
    public string OriginalName { get; init; } = string.Empty;

    /// <summary>
    ///     原始属性列表
    /// </summary>
    public List<(string Name, string Value)> RawAttributes { get; init; } = [];
}
