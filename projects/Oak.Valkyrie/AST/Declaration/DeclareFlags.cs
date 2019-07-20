using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     位标志枚举声明
/// </summary>
public sealed record DeclareFlags : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     标志枚举名称
    /// </summary>
    public IdentifierNode Name { get; init; } = new();

    /// <summary>
    ///     标志成员列表
    /// </summary>
    public IReadOnlyList<DeclareSemanticMember> Members { get; init; } = [];
}
