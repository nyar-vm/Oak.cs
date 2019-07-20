using Oak.Xml;

namespace Oak.Svg;

/// <summary>
///     SVG 文档解析器，基于 Oak.Xml 解析 SVG XML 结构
/// </summary>
public sealed class SvgParser
{
    /// <summary>
    ///     解析 SVG 文本内容
    /// </summary>
    public SvgDocument Parse(string content)
    {
        var xmlDoc = XmlParser.Parse(content);

        if (xmlDoc.Root is null)
        {
            throw new FormatException("SVG 解析失败：未找到根元素");
        }

        if (xmlDoc.Root.Name is not ("svg" or "svg:svg"))
        {
            throw new FormatException($"SVG 解析失败：根元素不是 svg，而是 {xmlDoc.Root.Name}");
        }

        var root = ParseSvgRoot(xmlDoc.Root);
        ParseChildren(xmlDoc.Root, root);

        return new SvgDocument { Root = root };
    }

    private SvgRootElement ParseSvgRoot(XmlElement element)
    {
        var viewBox = ParseViewBox(element.GetAttribute("viewBox"));
        var (width, widthUnit) = ParseLength(element.GetAttribute("width"));
        var (height, heightUnit) = ParseLength(element.GetAttribute("height"));

        return new SvgRootElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            ViewBox = viewBox,
            Width = width,
            Height = height,
            WidthUnit = widthUnit,
            HeightUnit = heightUnit
        };
    }

    private SvgElement ParseElement(XmlElement element)
    {
        var name = element.Name;

        if (name.Contains(':'))
        {
            name = name[(name.IndexOf(':') + 1)..];
        }

        SvgElement svgElement = name switch
        {
            "g" => ParseGroupElement(element),
            "path" => ParsePathElement(element),
            "rect" => ParseRectElement(element),
            "circle" => ParseCircleElement(element),
            "ellipse" => ParseEllipseElement(element),
            "line" => ParseLineElement(element),
            "polyline" => ParsePolylineElement(element),
            "polygon" => ParsePolygonElement(element),
            "text" => ParseTextElement(element),
            "defs" => ParseDefsElement(element),
            "use" => ParseUseElement(element),
            "image" => ParseImageElement(element),
            "linearGradient" => ParseLinearGradientElement(element),
            "radialGradient" => ParseRadialGradientElement(element),
            "clipPath" => ParseClipPathElement(element),
            "mask" => ParseMaskElement(element),
            _ => ParseUnknownElement(element)
        };

        ParseChildren(element, svgElement);

        return svgElement;
    }

    private void ParseChildren(XmlElement xmlElement, SvgElement svgElement)
    {
        foreach (var child in xmlElement.Children)
        {
            svgElement.Children.Add(ParseElement(child));
        }
    }

    #region 元素解析

    private SvgGroupElement ParseGroupElement(XmlElement element)
    {
        return new SvgGroupElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element)
        };
    }

    private SvgPathElement ParsePathElement(XmlElement element)
    {
        var d = element.GetAttribute("d") ?? string.Empty;

        return new SvgPathElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            Commands = SvgPathDataParser.Parse(d)
        };
    }

    private SvgRectElement ParseRectElement(XmlElement element)
    {
        return new SvgRectElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            X = ParseFloat(element.GetAttribute("x"), 0f),
            Y = ParseFloat(element.GetAttribute("y"), 0f),
            Width = ParseFloat(element.GetAttribute("width"), 0f),
            Height = ParseFloat(element.GetAttribute("height"), 0f),
            Rx = ParseFloat(element.GetAttribute("rx"), 0f),
            Ry = ParseFloat(element.GetAttribute("ry"), 0f)
        };
    }

    private SvgCircleElement ParseCircleElement(XmlElement element)
    {
        return new SvgCircleElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            Cx = ParseFloat(element.GetAttribute("cx"), 0f),
            Cy = ParseFloat(element.GetAttribute("cy"), 0f),
            R = ParseFloat(element.GetAttribute("r"), 0f)
        };
    }

    private SvgEllipseElement ParseEllipseElement(XmlElement element)
    {
        return new SvgEllipseElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            Cx = ParseFloat(element.GetAttribute("cx"), 0f),
            Cy = ParseFloat(element.GetAttribute("cy"), 0f),
            Rx = ParseFloat(element.GetAttribute("rx"), 0f),
            Ry = ParseFloat(element.GetAttribute("ry"), 0f)
        };
    }

    private SvgLineElement ParseLineElement(XmlElement element)
    {
        return new SvgLineElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            X1 = ParseFloat(element.GetAttribute("x1"), 0f),
            Y1 = ParseFloat(element.GetAttribute("y1"), 0f),
            X2 = ParseFloat(element.GetAttribute("x2"), 0f),
            Y2 = ParseFloat(element.GetAttribute("y2"), 0f)
        };
    }

    private SvgPolylineElement ParsePolylineElement(XmlElement element)
    {
        return new SvgPolylineElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            Points = ParsePoints(element.GetAttribute("points"))
        };
    }

    private SvgPolygonElement ParsePolygonElement(XmlElement element)
    {
        return new SvgPolygonElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            Points = ParsePoints(element.GetAttribute("points"))
        };
    }

    private SvgTextElement ParseTextElement(XmlElement element)
    {
        return new SvgTextElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            X = ParseFloat(element.GetAttribute("x"), 0f),
            Y = ParseFloat(element.GetAttribute("y"), 0f),
            Text = element.TextContent ?? string.Empty,
            FontFamily = element.GetAttribute("font-family") ?? string.Empty,
            FontSize = ParseFloat(element.GetAttribute("font-size"), 16f),
            TextAnchor = element.GetAttribute("text-anchor") ?? "start"
        };
    }

    private SvgDefsElement ParseDefsElement(XmlElement element)
    {
        return new SvgDefsElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element)
        };
    }

    private SvgUseElement ParseUseElement(XmlElement element)
    {
        var href = element.GetAttribute("href")
            ?? element.GetAttribute("xlink:href")
            ?? string.Empty;

        return new SvgUseElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            Href = href,
            X = ParseFloat(element.GetAttribute("x"), 0f),
            Y = ParseFloat(element.GetAttribute("y"), 0f),
            Width = ParseFloat(element.GetAttribute("width"), 0f),
            Height = ParseFloat(element.GetAttribute("height"), 0f)
        };
    }

    private SvgImageElement ParseImageElement(XmlElement element)
    {
        var href = element.GetAttribute("href")
            ?? element.GetAttribute("xlink:href")
            ?? string.Empty;

        return new SvgImageElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            Href = href,
            X = ParseFloat(element.GetAttribute("x"), 0f),
            Y = ParseFloat(element.GetAttribute("y"), 0f),
            Width = ParseFloat(element.GetAttribute("width"), 0f),
            Height = ParseFloat(element.GetAttribute("height"), 0f)
        };
    }

    private SvgLinearGradientElement ParseLinearGradientElement(XmlElement element)
    {
        return new SvgLinearGradientElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            X1 = ParseFloat(element.GetAttribute("x1"), 0f),
            Y1 = ParseFloat(element.GetAttribute("y1"), 0f),
            X2 = ParseFloat(element.GetAttribute("x2"), 1f),
            Y2 = ParseFloat(element.GetAttribute("y2"), 0f),
            GradientUnits = element.GetAttribute("gradientUnits") ?? "objectBoundingBox"
        };
    }

    private SvgRadialGradientElement ParseRadialGradientElement(XmlElement element)
    {
        var cx = ParseFloat(element.GetAttribute("cx"), 0.5f);
        var cy = ParseFloat(element.GetAttribute("cy"), 0.5f);

        return new SvgRadialGradientElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Cx = cx,
            Cy = cy,
            R = ParseFloat(element.GetAttribute("r"), 0.5f),
            Fx = ParseFloat(element.GetAttribute("fx"), cx),
            Fy = ParseFloat(element.GetAttribute("fy"), cy),
            GradientUnits = element.GetAttribute("gradientUnits") ?? "objectBoundingBox"
        };
    }

    private SvgClipPathElement ParseClipPathElement(XmlElement element)
    {
        return new SvgClipPathElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            ClipPathUnits = element.GetAttribute("clipPathUnits") ?? "userSpaceOnUse"
        };
    }

    private SvgMaskElement ParseMaskElement(XmlElement element)
    {
        return new SvgMaskElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            MaskUnits = element.GetAttribute("maskUnits") ?? "objectBoundingBox",
            MaskContentUnits = element.GetAttribute("maskContentUnits") ?? "userSpaceOnUse"
        };
    }

    private SvgUnknownElement ParseUnknownElement(XmlElement element)
    {
        var rawAttrs = new List<(string Name, string Value)>();

        foreach (var attr in element.Attributes)
        {
            rawAttrs.Add((attr.Name, attr.Value));
        }

        return new SvgUnknownElement
        {
            Id = element.GetAttribute("id") ?? string.Empty,
            Class = element.GetAttribute("class") ?? string.Empty,
            Transforms = ParseTransformAttribute(element.GetAttribute("transform")),
            Style = ParseStyleFromElement(element),
            OriginalName = element.Name,
            RawAttributes = rawAttrs
        };
    }

    #endregion

    #region 属性解析

    private static float[] ParseViewBox(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 4)
        {
            return [];
        }

        var viewBox = new float[4];

        for (var i = 0; i < 4; i++)
        {
            if (!float.TryParse(parts[i], out viewBox[i]))
            {
                return [];
            }
        }

        return viewBox;
    }

    private static (float Value, string Unit) ParseLength(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (0f, string.Empty);
        }

        var span = value.AsSpan();
        var i = 0;

        while (i < span.Length && (char.IsDigit(span[i]) || span[i] is '.' or '-' or '+' or 'e' or 'E'))
        {
            i++;
        }

        var numStr = span[..i];
        var unit = i < span.Length ? span[i..].ToString() : string.Empty;

        if (float.TryParse(numStr, out var num))
        {
            return (num, unit);
        }

        return (0f, string.Empty);
    }

    private static List<SvgTransform> ParseTransformAttribute(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return SvgTransformParser.Parse(value);
    }

    private static SvgStyle ParseStyleFromElement(XmlElement element)
    {
        var fill = element.GetAttribute("fill");
        var stroke = element.GetAttribute("stroke");
        var styleAttr = element.GetAttribute("style");

        if (!string.IsNullOrWhiteSpace(styleAttr))
        {
            return ParseStyleFromAttribute(styleAttr, fill, stroke);
        }

        return new SvgStyle
        {
            Fill = fill,
            FillOpacity = ParseFloat(element.GetAttribute("fill-opacity"), 1f),
            FillRule = element.GetAttribute("fill-rule") ?? "nonzero",
            Stroke = stroke,
            StrokeWidth = ParseFloat(element.GetAttribute("stroke-width"), 0f),
            StrokeOpacity = ParseFloat(element.GetAttribute("stroke-opacity"), 1f),
            StrokeLinecap = element.GetAttribute("stroke-linecap") ?? "butt",
            StrokeLinejoin = element.GetAttribute("stroke-linejoin") ?? "miter",
            StrokeDashoffset = ParseFloat(element.GetAttribute("stroke-dashoffset"), 0f),
            StrokeDasharray = ParseDasharray(element.GetAttribute("stroke-dasharray")),
            Opacity = ParseFloat(element.GetAttribute("opacity"), 1f)
        };
    }

    private static SvgStyle ParseStyleFromAttribute(string styleAttr, string? fill, string? stroke)
    {
        var style = new SvgStyle
        {
            Fill = fill,
            Stroke = stroke
        };

        var declarations = styleAttr.Split(';');

        foreach (var decl in declarations)
        {
            var colonIndex = decl.IndexOf(':');

            if (colonIndex < 0)
            {
                continue;
            }

            var property = decl[..colonIndex].Trim().ToLowerInvariant();
            var value = decl[(colonIndex + 1)..].Trim();

            style = property switch
            {
                "fill" => style with { Fill = value },
                "fill-opacity" => style with { FillOpacity = ParseFloat(value, 1f) },
                "fill-rule" => style with { FillRule = value },
                "stroke" => style with { Stroke = value },
                "stroke-width" => style with { StrokeWidth = ParseFloat(value, 0f) },
                "stroke-opacity" => style with { StrokeOpacity = ParseFloat(value, 1f) },
                "stroke-linecap" => style with { StrokeLinecap = value },
                "stroke-linejoin" => style with { StrokeLinejoin = value },
                "stroke-dashoffset" => style with { StrokeDashoffset = ParseFloat(value, 0f) },
                "stroke-dasharray" => style with { StrokeDasharray = ParseDasharray(value) },
                "opacity" => style with { Opacity = ParseFloat(value, 1f) },
                _ => style
            };
        }

        return style;
    }

    private static float[] ParsePoints(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var points = new List<float>();

        foreach (var part in parts)
        {
            var coords = part.Split(',');

            foreach (var coord in coords)
            {
                if (float.TryParse(coord, out var v))
                {
                    points.Add(v);
                }
            }
        }

        return points.ToArray();
    }

    private static float[] ParseDasharray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "none")
        {
            return [];
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<float>();

        foreach (var part in parts)
        {
            if (float.TryParse(part.TrimEnd(','), out var v))
            {
                result.Add(v);
            }
        }

        return result.ToArray();
    }

    private static float ParseFloat(string? value, float defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return float.TryParse(value, out var result) ? result : defaultValue;
    }

    #endregion
}
