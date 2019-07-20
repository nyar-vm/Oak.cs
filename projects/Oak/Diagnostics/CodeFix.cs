using Oak.Syntax;

namespace Oak.Diagnostics;

/// <summary>
///     代码修复建议数据结构
/// </summary>
public sealed class CodeFix
{
    /// <summary>
    ///     初始化代码修复建议
    /// </summary>
    /// <param name="description">修复描述</param>
    /// <param name="replacementText">替换文本</param>
    /// <param name="span">替换范围</param>
    public CodeFix(string description, string replacementText, TextSpan span)
    {
        Description = description;
        ReplacementText = replacementText;
        Span = span;
    }

    /// <summary>
    ///     修复描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     替换文本
    /// </summary>
    public string ReplacementText { get; }

    /// <summary>
    ///     替换范围
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    ///     返回修复描述字符串
    /// </summary>
    public override string ToString()
    {
        return $"{Description}: 替换 [{Span.Start}..{Span.End}) 为 '{ReplacementText}'";
    }
}
