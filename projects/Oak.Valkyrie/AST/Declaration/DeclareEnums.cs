using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     枚举声明
/// </summary>
public sealed record DeclareEnums : ValkyrieNode, IDeclarationNode
{
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     枚举名称
    /// </summary>
    public IdentifierNode Name { get; init; } = new();

    /// <summary>
    ///     枚举成员列表
    /// </summary>
    public IReadOnlyList<DeclareSemanticMember> Members { get; init; } = [];
}
