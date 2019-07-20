using Oak.Valkyrie.AST.Pattern;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     Match 匹配分支，由模式和对应代码体组成
/// </summary>
/// <para>示例：</para>
/// <code>
/// match value {
///     case 0:
///         print("零");        // Pattern = ConstantPattern(0), Body = { print("零"); }
///     case n if n &gt; 0:
///         ...                  // Pattern = DeclarationPattern("n"), Body = { ... }
///     case _:
///         print("未知");      // Pattern = WildcardPattern, Body = { ... }
/// }
/// </code>
public sealed record MatchArm : ValkyrieNode
{
    /// <summary>
    ///     匹配模式（<see cref="ConstantPattern"/> / <see cref="DeclarationPattern"/> / <see cref="PatternNode"/> / <see cref="WildcardPattern"/>）
    /// </summary>
    public ValkyrieNode Pattern { get; init; } = new IdentifierNode();

    /// <summary>
    ///     匹配成功时执行的代码体
    /// </summary>
    public FunctionBody Body { get; init; } = new();
}
