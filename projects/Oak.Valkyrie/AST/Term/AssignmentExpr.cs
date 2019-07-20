namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     赋值表达式
/// </summary>
/// <para>支持普通赋值和复合赋值运算：</para>
/// <code>
/// x = 42;          // Target = IdentifierNode("x"), Operator = "="
/// x += 1;          // Target = IdentifierNode("x"), Operator = "+="
/// x *= 2;          // Target = IdentifierNode("x"), Operator = "*="
/// </code>
public sealed record AssignmentExpr : ValkyrieNode
{
    /// <summary>
    ///     赋值目标（左值表达式）
    /// </summary>
    public ValkyrieNode Target { get; init; } = new IdentifierNode();

    /// <summary>
    ///     赋值运算符（<c>"="</c>、<c>"+="</c>、<c>"-="</c>、<c>"*="</c>、<c>"/="</c> 等）
    /// </summary>
    public string Operator { get; init; } = "=";

    /// <summary>
    ///     赋值的值表达式（右值）
    /// </summary>
    public ValkyrieNode Value { get; init; } = new IdentifierNode();
}
