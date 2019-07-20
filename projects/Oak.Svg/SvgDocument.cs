namespace Oak.Svg;

/// <summary>
///     SVG 文档，表示解析后的完整 SVG 内容
/// </summary>
public sealed class SvgDocument
{
    /// <summary>
    ///     SVG 根元素
    /// </summary>
    public SvgRootElement Root { get; init; } = new();

    /// <summary>
    ///     文档宽度（从根元素获取）
    /// </summary>
    public float Width => Root.Width;

    /// <summary>
    ///     文档高度（从根元素获取）
    /// </summary>
    public float Height => Root.Height;

    /// <summary>
    ///     视图框（从根元素获取）
    /// </summary>
    public float[] ViewBox => Root.ViewBox;

    /// <summary>
    ///     遍历所有元素（深度优先）
    /// </summary>
    public IEnumerable<SvgElement> EnumerateAll()
    {
        return EnumerateDescendants(Root);
    }

    /// <summary>
    ///     遍历指定类型的所有元素
    /// </summary>
    public IEnumerable<T> EnumerateOfType<T>() where T : SvgElement
    {
        foreach (var element in EnumerateAll())
        {
            if (element is T typed)
            {
                yield return typed;
            }
        }
    }

    /// <summary>
    ///     根据 ID 查找元素
    /// </summary>
    public SvgElement? FindById(string id)
    {
        foreach (var element in EnumerateAll())
        {
            if (element.Id == id)
            {
                return element;
            }
        }

        return null;
    }

    /// <summary>
    ///     获取所有可绘制元素（非容器、非定义元素）
    /// </summary>
    public IEnumerable<SvgElement> GetDrawableElements()
    {
        foreach (var element in EnumerateAll())
        {
            if (element is SvgDefsElement)
            {
                continue;
            }

            if (!element.IsContainer)
            {
                yield return element;
            }
        }
    }

    private static IEnumerable<SvgElement> EnumerateDescendants(SvgElement element)
    {
        yield return element;

        foreach (var child in element.Children)
        {
            foreach (var descendant in EnumerateDescendants(child))
            {
                yield return descendant;
            }
        }
    }
}
