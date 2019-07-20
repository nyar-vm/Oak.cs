namespace Oak.Svg;

/// <summary>
///     SVG 路径命令类型
/// </summary>
public enum SvgPathCommandType : byte
{
    /// <summary>
    ///     移动到（绝对）
    /// </summary>
    MoveTo = 0,

    /// <summary>
    ///     移动到（相对）
    /// </summary>
    RelativeMoveTo = 1,

    /// <summary>
    ///     直线到（绝对）
    /// </summary>
    LineTo = 2,

    /// <summary>
    ///     直线到（相对）
    /// </summary>
    RelativeLineTo = 3,

    /// <summary>
    ///     水平线到（绝对）
    /// </summary>
    HorizontalLineTo = 4,

    /// <summary>
    ///     水平线到（相对）
    /// </summary>
    RelativeHorizontalLineTo = 5,

    /// <summary>
    ///     垂直线到（绝对）
    /// </summary>
    VerticalLineTo = 6,

    /// <summary>
    ///     垂直线到（相对）
    /// </summary>
    RelativeVerticalLineTo = 7,

    /// <summary>
    ///     三次贝塞尔曲线（绝对）
    /// </summary>
    CurveTo = 8,

    /// <summary>
    ///     三次贝塞尔曲线（相对）
    /// </summary>
    RelativeCurveTo = 9,

    /// <summary>
    ///     平滑三次贝塞尔曲线（绝对）
    /// </summary>
    SmoothCurveTo = 10,

    /// <summary>
    ///     平滑三次贝塞尔曲线（相对）
    /// </summary>
    RelativeSmoothCurveTo = 11,

    /// <summary>
    ///     二次贝塞尔曲线（绝对）
    /// </summary>
    QuadraticCurveTo = 12,

    /// <summary>
    ///     二次贝塞尔曲线（相对）
    /// </summary>
    RelativeQuadraticCurveTo = 13,

    /// <summary>
    ///     平滑二次贝塞尔曲线（绝对）
    /// </summary>
    SmoothQuadraticCurveTo = 14,

    /// <summary>
    ///     平滑二次贝塞尔曲线（相对）
    /// </summary>
    RelativeSmoothQuadraticCurveTo = 15,

    /// <summary>
    ///     弧线（绝对）
    /// </summary>
    ArcTo = 16,

    /// <summary>
    ///     弧线（相对）
    /// </summary>
    RelativeArcTo = 17,

    /// <summary>
    ///     闭合路径
    /// </summary>
    ClosePath = 18
}

/// <summary>
///     SVG 路径命令
/// </summary>
public sealed class SvgPathCommand
{
    /// <summary>
    ///     命令类型
    /// </summary>
    public SvgPathCommandType Type { get; init; }

    /// <summary>
    ///     命令参数
    ///     <para>M/m: [x, y]</para>
    ///     <para>L/l: [x, y]</para>
    ///     <para>H/h: [x]</para>
    ///     <para>V/v: [y]</para>
    ///     <para>C/c: [x1, y1, x2, y2, x, y]</para>
    ///     <para>S/s: [x2, y2, x, y]</para>
    ///     <para>Q/q: [x1, y1, x, y]</para>
    ///     <para>T/t: [x, y]</para>
    ///     <para>A/a: [rx, ry, xRotation, largeArc, sweep, x, y]</para>
    ///     <para>Z/z: 无参数</para>
    /// </summary>
    public float[] Arguments { get; init; } = [];

    /// <summary>
    ///     是否为绝对坐标命令
    /// </summary>
    public bool IsAbsolute => Type is >= SvgPathCommandType.MoveTo and <= SvgPathCommandType.ArcTo
        && (int)Type % 2 == 0;
}

/// <summary>
///     SVG 路径数据解析器，解析 SVG path 元素的 d 属性
/// </summary>
public sealed class SvgPathDataParser
{
    /// <summary>
    ///     解析 SVG 路径数据字符串
    /// </summary>
    public static List<SvgPathCommand> Parse(string d)
    {
        if (string.IsNullOrWhiteSpace(d))
        {
            return [];
        }

        var commands = new List<SvgPathCommand>();
        var span = d.AsSpan();
        var i = 0;

        while (i < span.Length)
        {
            i = SkipWhitespace(span, i);

            if (i >= span.Length)
            {
                break;
            }

            var c = span[i];

            if (!IsCommandChar(c))
            {
                i++;
                continue;
            }

            var commandType = ParseCommandType(c);
            i++;

            if (commandType == SvgPathCommandType.ClosePath)
            {
                commands.Add(new SvgPathCommand { Type = commandType });
                continue;
            }

            var paramCount = GetParameterCount(commandType);

            while (i < span.Length)
            {
                var start = i;
                i = SkipWhitespace(span, i);

                if (i >= span.Length)
                {
                    break;
                }

                if (IsCommandChar(span[i]))
                {
                    break;
                }

                var args = new float[paramCount];
                var success = true;

                for (var p = 0; p < paramCount; p++)
                {
                    i = SkipWhitespace(span, i);

                    if (i >= span.Length)
                    {
                        success = false;
                        break;
                    }

                    if (!TryParseNumber(span, ref i, out var number))
                    {
                        success = false;
                        break;
                    }

                    args[p] = number;
                }

                if (!success)
                {
                    break;
                }

                commands.Add(new SvgPathCommand { Type = commandType, Arguments = args });

                if (commandType is SvgPathCommandType.MoveTo)
                {
                    commandType = SvgPathCommandType.LineTo;
                }
                else if (commandType is SvgPathCommandType.RelativeMoveTo)
                {
                    commandType = SvgPathCommandType.RelativeLineTo;
                }
            }
        }

        return commands;
    }

    private static SvgPathCommandType ParseCommandType(char c)
    {
        return c switch
        {
            'M' => SvgPathCommandType.MoveTo,
            'm' => SvgPathCommandType.RelativeMoveTo,
            'L' => SvgPathCommandType.LineTo,
            'l' => SvgPathCommandType.RelativeLineTo,
            'H' => SvgPathCommandType.HorizontalLineTo,
            'h' => SvgPathCommandType.RelativeHorizontalLineTo,
            'V' => SvgPathCommandType.VerticalLineTo,
            'v' => SvgPathCommandType.RelativeVerticalLineTo,
            'C' => SvgPathCommandType.CurveTo,
            'c' => SvgPathCommandType.RelativeCurveTo,
            'S' => SvgPathCommandType.SmoothCurveTo,
            's' => SvgPathCommandType.RelativeSmoothCurveTo,
            'Q' => SvgPathCommandType.QuadraticCurveTo,
            'q' => SvgPathCommandType.RelativeQuadraticCurveTo,
            'T' => SvgPathCommandType.SmoothQuadraticCurveTo,
            't' => SvgPathCommandType.RelativeSmoothQuadraticCurveTo,
            'A' => SvgPathCommandType.ArcTo,
            'a' => SvgPathCommandType.RelativeArcTo,
            'Z' or 'z' => SvgPathCommandType.ClosePath,
            _ => SvgPathCommandType.ClosePath
        };
    }

    private static int GetParameterCount(SvgPathCommandType type)
    {
        return type switch
        {
            SvgPathCommandType.MoveTo or SvgPathCommandType.RelativeMoveTo => 2,
            SvgPathCommandType.LineTo or SvgPathCommandType.RelativeLineTo => 2,
            SvgPathCommandType.HorizontalLineTo or SvgPathCommandType.RelativeHorizontalLineTo => 1,
            SvgPathCommandType.VerticalLineTo or SvgPathCommandType.RelativeVerticalLineTo => 1,
            SvgPathCommandType.CurveTo or SvgPathCommandType.RelativeCurveTo => 6,
            SvgPathCommandType.SmoothCurveTo or SvgPathCommandType.RelativeSmoothCurveTo => 4,
            SvgPathCommandType.QuadraticCurveTo or SvgPathCommandType.RelativeQuadraticCurveTo => 4,
            SvgPathCommandType.SmoothQuadraticCurveTo or SvgPathCommandType.RelativeSmoothQuadraticCurveTo => 2,
            SvgPathCommandType.ArcTo or SvgPathCommandType.RelativeArcTo => 7,
            _ => 0
        };
    }

    private static bool IsCommandChar(char c)
    {
        return c is 'M' or 'm' or 'L' or 'l' or 'H' or 'h' or 'V' or 'v'
            or 'C' or 'c' or 'S' or 's' or 'Q' or 'q' or 'T' or 't'
            or 'A' or 'a' or 'Z' or 'z';
    }

    private static int SkipWhitespace(ReadOnlySpan<char> span, int index)
    {
        while (index < span.Length && char.IsWhiteSpace(span[index]))
        {
            index++;
        }

        return index;
    }

    private static bool TryParseNumber(ReadOnlySpan<char> span, ref int index, out float value)
    {
        value = 0;

        if (index >= span.Length)
        {
            return false;
        }

        var start = index;
        var sign = 1f;

        if (span[index] is '-' or '+')
        {
            if (span[index] == '-') sign = -1;
            index++;
        }

        var hasDigits = false;
        var intPart = 0f;
        var fracPart = 0f;
        var fracDivisor = 1f;
        var hasExponent = false;
        var expSign = 1;
        var exponent = 0;

        while (index < span.Length && char.IsDigit(span[index]))
        {
            intPart = intPart * 10 + (span[index] - '0');
            hasDigits = true;
            index++;
        }

        if (index < span.Length && span[index] == '.')
        {
            index++;

            while (index < span.Length && char.IsDigit(span[index]))
            {
                fracPart = fracPart * 10 + (span[index] - '0');
                fracDivisor *= 10;
                hasDigits = true;
                index++;
            }
        }

        if (!hasDigits)
        {
            index = start;
            return false;
        }

        if (index < span.Length && (span[index] is 'e' or 'E'))
        {
            hasExponent = true;
            index++;

            if (index < span.Length && span[index] is '-' or '+')
            {
                if (span[index] == '-') expSign = -1;
                index++;
            }

            while (index < span.Length && char.IsDigit(span[index]))
            {
                exponent = exponent * 10 + (span[index] - '0');
                index++;
            }
        }

        value = sign * (intPart + fracPart / fracDivisor);

        if (hasExponent)
        {
            value *= MathF.Pow(10, expSign * exponent);
        }

        if (index < span.Length && span[index] is ',')
        {
            index++;
        }

        return true;
    }
}
