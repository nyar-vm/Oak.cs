using Oak.Syntax;

namespace Oak.Valkyrie.AST.Type;

/// <summary>
///     泛型约束，如 <c>where T : Foo, Bar</c> 中的单个约束项
/// </summary>
/// <para>示例：</para>
/// <code>
/// fn serialize&lt;T&gt;(value: T) where T : Serializable, Debug {
///     // GenericConstraints = [
///     //   GenericConstraint { ParameterName = "T", ConstraintTypes = [TypeAnnotation("Serializable"), TypeAnnotation("Debug")] }
///     // ]
/// }
/// </code>
public sealed record GenericConstraint : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public GenericConstraint() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public GenericConstraint(string parameterName, IReadOnlyList<TypeNode> constraintTypes, TextSpan span)
    {
        ParameterName = parameterName;
        ConstraintTypes = constraintTypes;
        Span = span;
    }

    /// <summary>
    ///     被约束的类型参数名称
    /// </summary>
    public string ParameterName { get; init; } = string.Empty;

    /// <summary>
    ///     约束类型列表（支持多约束，如 <c>where T : Foo, Bar</c>）
    /// </summary>
    public IReadOnlyList<TypeNode> ConstraintTypes { get; init; } = [];
}
