using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     Match 模式匹配语句，对表达式值进行多分支模式匹配
/// </summary>
/// <para>示例：</para>
/// <code>
/// match value {
///     case 0:
///         print("零");
///     case 1 | 2:
///         print("一或二");
///     case n if n &gt; 0:
///         print("正数 {n}");
///     case _:
///         print("未知");
/// }
/// </code>
public sealed record MatchStatement : ValkyrieNode
{
    /// <summary>
    ///     被匹配的表达式
    /// </summary>
    public ValkyrieNode Expression { get; init; } = new IdentifierNode();

    /// <summary>
    ///     匹配分支列表
    /// </summary>
    public IReadOnlyList<MatchArm> Arms { get; init; } = [];
}
