namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
/// 
/// </summary>
/// <para>示例：</para>
/// <code>
/// {
///     field: Type = default
///     method(input: Input) -> Output { ... }
///     domain { ... }
/// }
/// </code>
public sealed record ObjectBody : ValkyrieNode
{
    /// <summary>
    ///     字段列表
    /// </summary>
    public IReadOnlyList<DeclareObjectField> Fields { get; init; } = [];

    /// <summary>
    ///     函数列表
    /// </summary>
    public IReadOnlyList<DeclareObjectMethod> Methods { get; init; } = [];

    /// <summary>
    ///     子域列表
    /// </summary>
    public IReadOnlyList<DeclareObjectDomain> Domains { get; init; } = [];
}