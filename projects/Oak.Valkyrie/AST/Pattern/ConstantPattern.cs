using Oak.Syntax;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Pattern;

/// <summary>
///     常量模式 —— 匹配字面常量值
/// </summary>
/// <para>示例：</para>
/// <code>
/// match x {
///     case 42:
///         ...          // ConstantPattern { Value = LiteralExpr(42) }
///     case true:
///         ...          // ConstantPattern { Value = LiteralExpr(true) }
///     case "hello":
///         ...          // ConstantPattern { Value = LiteralExpr("hello") }
/// }
/// </code>
public sealed record ConstantPattern : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public ConstantPattern() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public ConstantPattern(TermAtomicLiteral value, TextSpan span)
    {
        Value = value;
        Span = span;
    }

    /// <summary>
    ///     匹配的常量值
    /// </summary>
    public TermAtomicLiteral Value { get; init; } = new();
}
