namespace Oak.Syntax;

/// <summary>
///     语法高亮区间 —— 源码中的一段文本及其高亮类别
/// </summary>
public readonly struct HighlightSpan
{
    /// <summary>
    ///     高亮类别
    /// </summary>
    public HighlightKind Kind { get; init; }

    /// <summary>
    ///     在源码中的起始位置
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    ///     文本长度
    /// </summary>
    public int Length { get; init; }
}
