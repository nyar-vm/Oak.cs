using Oak.Syntax;

namespace Oak.Valkyrie.AST.Pattern;

/// <summary>
///     通配符模式 —— 匹配任意值，相当于 <c>match</c> 语句中的默认分支
/// </summary>
/// <para>示例：</para>
/// <code>
/// match value {
///     case 1:
///         print("一");
///     case _:
///         print("其他");    // WildcardPattern — 匹配所有未被前序分支匹配的值
/// }
/// </code>
public sealed record WildcardPattern : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public WildcardPattern() { }

    /// <summary>
    ///     带位置的构造函数
    /// </summary>
    public WildcardPattern(TextSpan span)
    {
        Span = span;
    }
}
