using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     联合类型（Unite）声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// unite Option<T> {
///     Some { value: T },
///     None,
/// }
/// </code>
public sealed record DeclareUnite : ValkyrieNode, IDeclarationNode
{
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     联合类型名称
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     变体列表
    /// </summary>
    public IReadOnlyList<DeclareUniteVariant> Variants { get; init; } = [];

    /// <summary>
    ///     函数列表
    /// </summary>
    public IReadOnlyList<DeclareObjectMethod> Methods { get; init; } = [];

}
