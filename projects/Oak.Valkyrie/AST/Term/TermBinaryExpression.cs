namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     二元运算表达式
/// </summary>
/// <para>支持所有二元运算符：</para>
/// <code>
/// a + b     // Left = IdentifierNode("a"), Operator = "+", Right = IdentifierNode("b")
/// x &gt; 0  // Operator = "&gt;"
/// a &amp;&amp; b  // Operator = "&amp;&amp;"
/// </code>
public sealed record TermBinaryExpression : ValkyrieNode
{
    /// <summary>
    ///     左操作数
    /// </summary>
    public ValkyrieNode Left { get; init; } = new IdentifierNode();

    /// <summary>
    ///     运算符（如 <c>"+"</c>、<c>"-"</c>、<c>"*"</c>、<c>"&gt;"</c>、<c>"=="</c>、<c>"&amp;&amp;"</c> 等）
    /// </summary>
    public string Operator { get; init; } = string.Empty;

    /// <summary>
    ///     右操作数
    /// </summary>
    public ValkyrieNode Right { get; init; } = new IdentifierNode();
}
