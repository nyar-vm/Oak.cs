using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     函数参数声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro greet(name: utf8, times: i32) { ... }
/// </code>
public sealed record ParameterList : ValkyrieNode, IDeclarationNode
{
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     参数名称（可为空，表示匿名参数）
    /// </summary>
    public IdentifierNode? Name { get; init; }

    /// <summary>
    ///     参数类型
    /// </summary>
    public TypeNode ParamType { get; init; } = new();
}
