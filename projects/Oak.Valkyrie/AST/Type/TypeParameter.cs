using Oak.Syntax;

namespace Oak.Valkyrie.AST.Type;

/// <summary>
///     泛型类型参数声明，如 <c>T</c>、<c>U</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// fn swap&lt;T&gt;(a: T, b: T)
/// //        ^  TypeParameter { Name = "T", Constraints = [] }
/// </code>
public sealed record TypeParameter : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TypeParameter() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TypeParameter(string name, IReadOnlyList<GenericConstraint> constraints, TextSpan span)
    {
        Name = name;
        Constraints = constraints;
        Span = span;
    }

    /// <summary>
    ///     类型参数名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     关联的 <c>where</c> 约束列表
    /// </summary>
    public IReadOnlyList<GenericConstraint> Constraints { get; init; } = [];
}
