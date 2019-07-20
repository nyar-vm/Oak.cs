namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
/// 
/// </summary>
public sealed record AttributeList : ValkyrieNode
{
    /// <summary>
    ///     属性参数列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Items { get; init; } = [];
}