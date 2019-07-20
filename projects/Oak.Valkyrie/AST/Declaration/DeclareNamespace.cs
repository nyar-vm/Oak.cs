using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     命名空间声明
/// </summary>
public sealed record DeclareNamespace : ValkyrieNode
{
    /// <summary>
    ///     命名空间名称（点号分隔）
    /// </summary>
    public IdentifierNode Name { get; init; } = new();

    /// <summary>
    ///     是否为主命名空间
    /// </summary>
    public bool IsPrimary { get; init; }

    /// <summary>
    ///     是否为测试命名空间
    /// </summary>
    public bool IsTest { get; init; }

    /// <summary>
    ///     命名空间内的声明列表
    /// </summary>
    public IReadOnlyList<ValkyrieNode> Declarations { get; init; } = [];

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];
}
