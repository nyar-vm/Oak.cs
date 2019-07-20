using Oak.Syntax;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     Trait 声明节点，如 <c>trait Numeric { ... }</c>
///     定义一组方法签名，作为泛型约束使用
/// </summary>
public sealed record DeclareTrait : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType Type => ValkyrieNodeType.TraitDecl;

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     Trait 名称
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     泛型类型参数
    /// </summary>
    public IReadOnlyList<TypeParameter> TypeParameters { get; init; } = [];

    /// <summary>
    ///     泛型约束
    /// </summary>
    public IReadOnlyList<GenericConstraint> GenericConstraints { get; init; } = [];

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? Body { get; init; } = null;
}
