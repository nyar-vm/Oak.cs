using Oak.Syntax;
using Oak.Valkyrie.AST.Statement;
using Oak.Valkyrie.AST.Template;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     函数声明节点
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro add(x: i32, y: i32) -> i32 {
///     return x + y;
/// }
/// </code>
/// <para>支持泛型：</para>
/// <code>
/// micro identity&lt;T&gt;(value: T) -> T {
///     return value;
/// }
/// </code>
/// <para>支持属性：</para>
/// <code>
/// [import("std.math", "min")]
/// micro min() { ... }
/// </code>
public sealed record DeclareMicro : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     函数名称
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     参数列表
    /// </summary>
    public IReadOnlyList<ParameterList> Parameters { get; init; } = [];

    /// <summary>
    ///     返回值类型注解，为 <c>null</c> 时表示无返回值
    /// </summary>
    public TypeNode? ReturnType { get; init; }

    /// <summary>
    ///     函数体代码块，外部函数/抽象函数可为 <c>null</c>
    /// </summary>
    public FunctionBody? Body { get; init; }

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();


    /// <summary>
    ///     泛型类型参数列表
    /// </summary>
    public IReadOnlyList<TypeParameter> TypeParameters { get; init; } = [];

    /// <summary>
    ///     泛型约束列表（<c>where</c> 子句）
    /// </summary>
    public IReadOnlyList<GenericConstraint> GenericConstraints { get; init; } = [];
}
