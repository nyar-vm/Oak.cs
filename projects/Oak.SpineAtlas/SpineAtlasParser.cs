using System.Globalization;

namespace Oak.SpineAtlas;

/// <summary>
///     Spine Atlas 文本格式解析器
/// </summary>
public static class SpineAtlasParser
{
    /// <summary>
    ///     解析 Spine Atlas 文本
    /// </summary>
    public static SpineAtlasData Parse(string atlasContent)
    {
        var pages = new List<SpineAtlasPage>();
        var regions = new List<SpineAtlasRegion>();

        using var reader = new StringReader(atlasContent);
        var line = reader.ReadLine();
        var currentPageIndex = -1;

        while (line != null)
        {
            line = line.Trim();

            if (string.IsNullOrEmpty(line))
            {
                line = reader.ReadLine();
                continue;
            }

            if (!line.Contains(':'))
            {
                if (currentPageIndex >= 0) regions.Add(ParseRegion(line, reader, currentPageIndex));
            }
            else if (line.StartsWith("size:", StringComparison.OrdinalIgnoreCase))
            {
                currentPageIndex++;
                pages.Add(ParsePage(line, reader));
            }

            line = reader.ReadLine();
        }

        return new SpineAtlasData
        {
            Pages = pages,
            Regions = regions
        };
    }

    private static SpineAtlasPage ParsePage(string firstLine, StringReader reader)
    {
        var textureFilePath = string.Empty;
        var width = 0;
        var height = 0;
        var format = string.Empty;
        var filterMin = string.Empty;
        var filterMag = string.Empty;
        var wrapS = "clampToEdge";
        var wrapT = "clampToEdge";

        var parts = firstLine.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            var sizeParts = parts[1].Split(',', StringSplitOptions.TrimEntries);
            if (sizeParts.Length >= 2)
            {
                int.TryParse(sizeParts[0], out width);
                int.TryParse(sizeParts[1], out height);
            }
        }

        var line = reader.ReadLine();
        while (line != null && !string.IsNullOrWhiteSpace(line) && line.Contains(':'))
        {
            line = line.Trim();
            var kv = line.Split(':', StringSplitOptions.TrimEntries);
            if (kv.Length >= 2)
            {
                var key = kv[0].ToLowerInvariant();
                var value = kv[1];

                switch (key)
                {
                    case "format":
                        format = value;
                        break;
                    case "filter":
                        var filters = value.Split(',', StringSplitOptions.TrimEntries);
                        filterMin = filters.Length > 0 ? filters[0] : value;
                        filterMag = filters.Length > 1 ? filters[1] : filterMin;
                        break;
                    case "repeat":
                        wrapS = value.ToLowerInvariant() switch
                        {
                            "x" => "repeat",
                            "y" => "repeat",
                            "xy" => "repeat",
                            _ => "clampToEdge"
                        };
                        wrapT = value.ToLowerInvariant() switch
                        {
                            "x" => "clampToEdge",
                            "y" => "clampToEdge",
                            "xy" => "repeat",
                            _ => "clampToEdge"
                        };
                        break;
                }
            }

            line = reader.ReadLine();
        }

        return new SpineAtlasPage
        {
            TextureFilePath = textureFilePath,
            Width = width,
            Height = height,
            Format = format,
            FilterMin = filterMin,
            FilterMag = filterMag,
            WrapS = wrapS,
            WrapT = wrapT
        };
    }

    private static SpineAtlasRegion ParseRegion(string name, StringReader reader, int pageIndex)
    {
        var region = new SpineAtlasRegion
        {
            Name = name,
            PageIndex = pageIndex
        };

        var line = reader.ReadLine();
        while (line != null && !string.IsNullOrWhiteSpace(line) && line.Contains(':'))
        {
            line = line.Trim();
            var kv = line.Split(':', StringSplitOptions.TrimEntries);
            if (kv.Length >= 2)
            {
                var key = kv[0].ToLowerInvariant();
                var value = kv[1];

                switch (key)
                {
                    case "bounds":
                    case "xy":
                        var xyParts = value.Split(',', StringSplitOptions.TrimEntries);
                        if (xyParts.Length >= 2)
                        {
                            int.TryParse(xyParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x);
                            int.TryParse(xyParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y);
                            region = region with { X = x, Y = y };
                        }

                        break;
                    case "size":
                        var sizeParts = value.Split(',', StringSplitOptions.TrimEntries);
                        if (sizeParts.Length >= 2)
                        {
                            int.TryParse(sizeParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var w);
                            int.TryParse(sizeParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var h);
                            region = region with { Width = w, Height = h };
                        }

                        break;
                    case "offset":
                        var offsetParts = value.Split(',', StringSplitOptions.TrimEntries);
                        if (offsetParts.Length >= 2)
                        {
                            int.TryParse(offsetParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture,
                                out var ox);
                            int.TryParse(offsetParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture,
                                out var oy);
                            region = region with { OffsetX = ox, OffsetY = oy };
                        }

                        break;
                    case "orig":
                    case "original":
                        var origParts = value.Split(',', StringSplitOptions.TrimEntries);
                        if (origParts.Length >= 2)
                        {
                            int.TryParse(origParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ow);
                            int.TryParse(origParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var oh);
                            region = region with { OriginalWidth = ow, OriginalHeight = oh };
                        }

                        break;
                    case "rotate":
                        region = region with
                        {
                            IsRotated = value.Equals("90", StringComparison.OrdinalIgnoreCase)
                                        || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                        };
                        break;
                    case "split":
                        region = region with { IsSplit = true, Splits = ParseIntArray(value) };
                        break;
                    case "pad":
                        region = region with { Pads = ParseIntArray(value) };
                        break;
                }
            }

            line = reader.ReadLine();
        }

        return region;
    }

    private static int[] ParseIntArray(string value)
    {
        return value.Split(',', StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0)
            .ToArray();
    }
}