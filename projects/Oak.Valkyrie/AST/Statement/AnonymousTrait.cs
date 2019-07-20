using Oak.Syntax;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     Trait 声明节点，如 <c>trait { ... }</c>
///     定义一组方法签名，作为泛型约束使用
/// </summary>
public sealed record AnonymousTrait : ValkyrieNode
{
    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType Type => ValkyrieNodeType.TraitDecl;

    /// <summary>
    ///     无参构造函数
    /// </summary>
    public AnonymousTrait()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public AnonymousTrait(IReadOnlyList<DeclareMicro> methods, IReadOnlyList<TypeParameter> typeParameters,
        IReadOnlyList<GenericConstraint> genericConstraints, TextSpan span)
    {
        var objectMethods = new List<DeclareObjectMethod>(methods.Count);
        foreach (var method in methods)
        {
            objectMethods.Add(new DeclareObjectMethod
            {
                Name = method.Name,
                Annotations = method.Annotations
            });
        }

        Body = new ObjectBody
        {
            Methods = objectMethods
        };
        TypeParameters = typeParameters;
        GenericConstraints = genericConstraints;
        Span = span;
    }

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
