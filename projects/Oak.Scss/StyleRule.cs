namespace Oak.Scss;

/// <summary>
///     样式规则
/// </summary>
public sealed class StyleRule
{
    public StyleRule(IReadOnlyList<StyleSelector> selectors, IReadOnlyList<StyleDeclaration> declarations)
    {
        Selectors = selectors;
        Declarations = declarations;
        Specificity = ComputeSpecificity(selectors);
    }

    /// <summary>
    ///     选择器列表
    /// </summary>
    public IReadOnlyList<StyleSelector> Selectors { get; }

    /// <summary>
    ///     声明列表
    /// </summary>
    public IReadOnlyList<StyleDeclaration> Declarations { get; }

    /// <summary>
    ///     特异性权重
    /// </summary>
    public int Specificity { get; }

    private static int ComputeSpecificity(IReadOnlyList<StyleSelector> selectors)
    {
        var ids = 0;
        var classes = 0;
        var types = 0;

        foreach (var selector in selectors)
            switch (selector.Type)
            {
                case StyleSelectorType.Id:
                    ids++;
                    break;
                case StyleSelectorType.Class:
                case StyleSelectorType.PseudoClass:
                    classes++;
                    break;
                case StyleSelectorType.Type:
                    types++;
                    break;
                case StyleSelectorType.ParentRef:
                    break;
            }

        return (ids << 16) | (classes << 8) | types;
    }
}