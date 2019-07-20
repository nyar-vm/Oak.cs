using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     函数调用表达式，如 <c>func(arg1, arg2)</c>
/// </summary>
/// <para>支持泛型函数调用：</para>
/// <code>
/// print("hello");                      // Callee = IdentifierNode("print"), Arguments = [LiteralExpr("hello")]
/// add::&lt;int&gt;(a, b);              // Callee = IdentifierNode("add"), TypeArguments = [TypeAnnotation("int")]
/// obj.method(x, y);                    // Callee = MemberAccessExpr { Target = IdentifierNode("obj"), MemberName = "method" }
/// </code>
public sealed record TermCallExpression : ValkyrieNode
{
    /// <summary>
    ///     被调用的表达式（函数名或成员访问表达式）
    /// </summary>
    public ValkyrieNode Callee { get; init; } = new IdentifierNode();

    /// <summary>
    ///     显式指定的泛型类型参数列表（<c>::&lt;T&gt;</c> 形式）
    /// </summary>
    public IReadOnlyList<TypeNode> TypeArguments { get; init; } = [];

    /// <summary>
    ///     调用时传入的实际参数列表
    /// </summary>
    public IReadOnlyList<ValkyrieNode> Arguments { get; init; } = [];
}
