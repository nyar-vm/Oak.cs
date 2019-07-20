using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     Turbofish 表达式 —— <c>func::&lt;T&gt;</c> 形式，用于显式指定泛型参数
/// </summary>
/// <para>示例：</para>
/// <code>
/// collect::&lt;int&gt;(items);            // Target = IdentifierNode("collect"), TypeArguments = [TypeAnnotation("int")]
/// parse::&lt;i32&gt;(input);             // Target = IdentifierNode("parse"), TypeArguments = [TypeAnnotation("i32")]
/// Vec::&lt;f64&gt;::new();               // Target = QualifiedPathExpr("Vec"), TypeArguments = [TypeAnnotation("f64")]
/// </code>
public sealed record TurbofishExpr : ValkyrieNode
{
    /// <summary>
    ///     目标表达式（函数/类型名）
    /// </summary>
    public ValkyrieNode Target { get; init; } = new IdentifierNode();

    /// <summary>
    ///     类型参数列表
    /// </summary>
    public IReadOnlyList<TypeNode> TypeArguments { get; init; } = [];
}
