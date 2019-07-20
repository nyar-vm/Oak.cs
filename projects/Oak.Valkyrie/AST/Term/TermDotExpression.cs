namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     成员访问表达式，如 <c>object.member</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// player.name             // Target = IdentifierNode("player"), MemberName = "name"
/// self.health             // Target = IdentifierNode("self"), MemberName = "health"
/// instance.method()       // 后续 TermCallExpression 引用此表达式作为 Callee
/// </code>
public sealed record TermDotExpression : ValkyrieNode
{
    /// <summary>
    ///     访问的目标对象（左.之前的部分）
    /// </summary>
    public ValkyrieNode Target { get; init; } = new IdentifierNode();

    /// <summary>
    ///     要访问的成员名称
    /// </summary>
    public string MemberName { get; init; } = string.Empty;
}
