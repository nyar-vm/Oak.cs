namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     一元运算表达式（前缀或后缀）
/// </summary>
/// <para>支持以下一元运算符：</para>
/// <code>
/// -x              // Operator = "-",    Operand = IdentifierNode("x"), IsPrefix = true
/// !flag           // Operator = "!",    Operand = IdentifierNode("flag"), IsPrefix = true
/// i++             // Operator = "++",   Operand = IdentifierNode("i"), IsPrefix = false
/// </code>
public sealed record TermUnaryExpression : ValkyrieNode
{
    /// <summary>
    ///     一元运算符（如 <c>"-"</c>、<c>"!"</c>、<c>"~"</c>、<c>"++"</c>、<c>"--"</c>）
    /// </summary>
    public string Operator { get; init; } = string.Empty;

    /// <summary>
    ///     操作数
    /// </summary>
    public ValkyrieNode Operand { get; init; } = new IdentifierNode();

    /// <summary>
    ///     是否为前缀运算符（<c>true</c> 表示前置如 <c>-x</c>，<c>false</c> 表示后置如 <c>x++</c>）
    /// </summary>
    public bool IsPrefix { get; init; } = true;
}
