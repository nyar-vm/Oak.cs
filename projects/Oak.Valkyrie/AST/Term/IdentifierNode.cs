namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     标识符节点，表示源代码中的一个名称引用
/// </summary>
/// <para>用于变量引用、类型名、函数名等所有命名引用场景</para>
/// <para>示例：</para>
/// <code>
/// var x = 42;        // Name = "x"
/// var player = ...;  // Name = "player"
/// player.health      // 作为 MemberAccessExpr 的子节点时 Name = "player"
/// </code>
public sealed record IdentifierNode : ValkyrieNode
{
    /// <summary>
    ///     标识符名称
    /// </summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// `raw identifier`
    /// </summary>
    public bool IsRaw { get; init; } = false;
    public IdentifierNode() { }
    public IdentifierNode(string name, bool isRaw = false)
    {
        Name = name;
        IsRaw = isRaw;
    }
}