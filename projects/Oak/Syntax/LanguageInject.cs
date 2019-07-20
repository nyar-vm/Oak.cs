using Oak.Syntax;
namespace Oak.Syntax;

/// <summary>
///     语言注入辅助，支持在解析过程中注入子语言解析器
/// </summary>
public static class LanguageInject
{
    /// <summary>
    ///     注入子语言解析器，从 LanguageRegistry 获取对应解析器并产出子 GreenNode
    /// </summary>
    /// <param name="languageId">子语言标识符</param>
    /// <param name="source">子语言源文本</param>
    /// <param name="baseOffset">注入点在主源文本中的基偏移</param>
    /// <returns>子语言的 GreenNode，若语言未注册则返回 null</returns>
    public static GreenNode? Inject(string languageId, ISource source, int baseOffset = 0)
    {
        if (!LanguageRegistry.IsRegistered(languageId)) return null;

        var root = LanguageRegistry.Parse(languageId, source);
        return root.Green;
    }

    /// <summary>
    ///     注入子语言解析器，从主源文本的指定范围中提取子源文本
    /// </summary>
    /// <param name="languageId">子语言标识符</param>
    /// <param name="mainSource">主源文本</param>
    /// <param name="range">子语言在主源文本中的范围</param>
    /// <returns>子语言的 GreenNode，若语言未注册则返回 null</returns>
    public static GreenNode? Inject(string languageId, ISource mainSource, TextSpan range)
    {
        var subText = mainSource.Substring(new Range(range.Start, range.End));
        var subSource = new StringSource(subText);
        return Inject(languageId, subSource, range.Start);
    }
}