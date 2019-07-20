namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     继承规范，描述类继承的基类或实现的接口
/// </summary>
/// <para>示例：</para>
/// <code>
/// class Player() { ...}
/// class Player(Base) { ...}
/// class Player(base1: Base, base2: Base) { ...} 
/// </code>
public sealed record InheritanceList : ValkyrieNode
{
    /// <summary>
    ///     基类实例对应的有效字段名，用于 <c>self.base</c> 访问
    /// </summary>
    public IReadOnlyList<InheritanceItem> Bases { get; init; } = [];

    public InheritanceList(IReadOnlyList<InheritanceItem>? bases = null)
    {
        Bases = bases ?? [];
    }
}