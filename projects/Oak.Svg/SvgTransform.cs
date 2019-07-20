namespace Oak.Svg;

/// <summary>
///     SVG 变换类型
/// </summary>
public enum SvgTransformType : byte
{
    /// <summary>
    ///     矩阵变换
    /// </summary>
    Matrix = 0,

    /// <summary>
    ///     平移
    /// </summary>
    Translate = 1,

    /// <summary>
    ///     缩放
    /// </summary>
    Scale = 2,

    /// <summary>
    ///     旋转
    /// </summary>
    Rotate = 3,

    /// <summary>
    ///     X 轴倾斜
    /// </summary>
    SkewX = 4,

    /// <summary>
    ///     Y 轴倾斜
    /// </summary>
    SkewY = 5
}

/// <summary>
///     SVG 变换操作
/// </summary>
public sealed class SvgTransform
{
    /// <summary>
    ///     变换类型
    /// </summary>
    public SvgTransformType Type { get; init; }

    /// <summary>
    ///     变换参数
    ///     <para>Matrix: [a, b, c, d, e, f]</para>
    ///     <para>Translate: [tx, ty?]（ty 默认 0）</para>
    ///     <para>Scale: [sx, sy?]（sy 默认等于 sx）</para>
    ///     <para>Rotate: [angle, cx?, cy?]（cx/cy 默认 0）</para>
    ///     <para>SkewX: [angle]</para>
    ///     <para>SkewY: [angle]</para>
    /// </summary>
    public float[] Arguments { get; init; } = [];

    /// <summary>
    ///     转换为 3x2 仿射变换矩阵 [a, b, c, d, e, f]
    /// </summary>
    public float[] ToMatrix()
    {
        return Type switch
        {
            SvgTransformType.Matrix => Arguments.Length >= 6
                ? [Arguments[0], Arguments[1], Arguments[2], Arguments[3], Arguments[4], Arguments[5]]
                : [1f, 0f, 0f, 1f, 0f, 0f],
            SvgTransformType.Translate => Arguments.Length >= 2
                ? [1f, 0f, 0f, 1f, Arguments[0], Arguments[1]]
                : Arguments.Length >= 1
                    ? [1f, 0f, 0f, 1f, Arguments[0], 0f]
                    : [1f, 0f, 0f, 1f, 0f, 0f],
            SvgTransformType.Scale => Arguments.Length >= 2
                ? [Arguments[0], 0f, 0f, Arguments[1], 0f, 0f]
                : Arguments.Length >= 1
                    ? [Arguments[0], 0f, 0f, Arguments[0], 0f, 0f]
                    : [1f, 0f, 0f, 1f, 0f, 0f],
            SvgTransformType.Rotate => ComputeRotationMatrix(),
            SvgTransformType.SkewX => Arguments.Length >= 1
                ? [1f, 0f, MathF.Tan(Arguments[0] * MathF.PI / 180f), 1f, 0f, 0f]
                : [1f, 0f, 0f, 1f, 0f, 0f],
            SvgTransformType.SkewY => Arguments.Length >= 1
                ? [1f, MathF.Tan(Arguments[0] * MathF.PI / 180f), 0f, 1f, 0f, 0f]
                : [1f, 0f, 0f, 1f, 0f, 0f],
            _ => [1f, 0f, 0f, 1f, 0f, 0f]
        };
    }

    private float[] ComputeRotationMatrix()
    {
        if (Arguments.Length < 1)
        {
            return [1f, 0f, 0f, 1f, 0f, 0f];
        }

        var angle = Arguments[0] * MathF.PI / 180f;
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);

        if (Arguments.Length >= 3)
        {
            var cx = Arguments[1];
            var cy = Arguments[2];
            return [cos, sin, -sin, cos, cx * (1 - cos) - cy * sin, cy * (1 - cos) + cx * sin];
        }

        return [cos, sin, -sin, cos, 0f, 0f];
    }
}

/// <summary>
///     SVG 变换解析器，解析 transform 属性
/// </summary>
public sealed class SvgTransformParser
{
    /// <summary>
    ///     解析 SVG transform 属性字符串
    /// </summary>
    public static List<SvgTransform> Parse(string transform)
    {
        if (string.IsNullOrWhiteSpace(transform))
        {
            return [];
        }

        var results = new List<SvgTransform>();
        var span = transform.AsSpan();
        var i = 0;

        while (i < span.Length)
        {
            while (i < span.Length && !char.IsLetter(span[i]))
            {
                i++;
            }

            if (i >= span.Length)
            {
                break;
            }

            var nameStart = i;
            while (i < span.Length && char.IsLetter(span[i]))
            {
                i++;
            }

            var name = span[nameStart..i].ToString();

            while (i < span.Length && span[i] != '(')
            {
                i++;
            }

            if (i >= span.Length)
            {
                break;
            }

            i++;

            var argsStart = i;
            var depth = 1;

            while (i < span.Length && depth > 0)
            {
                if (span[i] == '(') depth++;
                else if (span[i] == ')') depth--;
                i++;
            }

            var argsStr = span[argsStart..(i - 1)].ToString();
            var args = ParseArguments(argsStr);

            var transformType = name.ToLowerInvariant() switch
            {
                "matrix" => SvgTransformType.Matrix,
                "translate" => SvgTransformType.Translate,
                "scale" => SvgTransformType.Scale,
                "rotate" => SvgTransformType.Rotate,
                "skewx" => SvgTransformType.SkewX,
                "skewy" => SvgTransformType.SkewY,
                _ => (SvgTransformType?)null
            };

            if (transformType.HasValue)
            {
                results.Add(new SvgTransform { Type = transformType.Value, Arguments = args });
            }
        }

        return results;
    }

    private static float[] ParseArguments(string argsStr)
    {
        var args = new List<float>();
        var span = argsStr.AsSpan();
        var i = 0;

        while (i < span.Length)
        {
            while (i < span.Length && (char.IsWhiteSpace(span[i]) || span[i] is ',' or ';'))
            {
                i++;
            }

            if (i >= span.Length)
            {
                break;
            }

            var start = i;

            if (span[i] is '-' or '+')
            {
                i++;
            }

            while (i < span.Length && (char.IsDigit(span[i]) || span[i] is '.' or 'e' or 'E' or '-' or '+'))
            {
                i++;
            }

            var numStr = span[start..i];

            if (float.TryParse(numStr, out var value))
            {
                args.Add(value);
            }
        }

        return args.ToArray();
    }
}
