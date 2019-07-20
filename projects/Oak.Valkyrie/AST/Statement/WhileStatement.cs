using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     While 循环语句，条件为真时重复执行循环体
/// </summary>
/// <para>示例：</para>
/// <code>
/// while i &gt; 0 {
///     print(i);
///     i -= 1;
/// }
/// </code>
public sealed record WhileStatement : ValkyrieNode
{
    /// <summary>
    ///     循环条件表达式
    /// </summary>
    public ValkyrieNode Condition { get; init; } = new IdentifierNode();

    /// <summary>
    ///     循环体
    /// </summary>
    public FunctionBody Body { get; init; } = new();
}
