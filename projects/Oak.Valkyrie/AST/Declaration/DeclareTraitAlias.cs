using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     类型别名声明，为现有类型创建新名称
/// </summary>
/// <para>示例：</para>
/// <code>
/// trait PlayerId = int;
/// trait Callback = { to_float(i32) -> f32 };
/// </code>
public sealed record DeclareTraitAlias : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    /// 
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     目标类型
    /// </summary>
    public TypeNode TargetType { get; init; } = new();
}
