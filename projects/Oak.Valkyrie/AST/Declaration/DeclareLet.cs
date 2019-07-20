using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     变量声明语句
/// </summary>
/// <para>示例：</para>
/// <code>
/// let x: i32 = 42;
/// let mut y = 0;            // IsMutable = true
/// let z;                    // VarType = null, Initializer = null
/// </code>
public sealed record DeclareLet : ValkyrieNode
{
    /// <summary>
    ///     变量名称
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     是否为可变变量（<c>var mut</c>）
    /// </summary>
    public bool IsMutable { get; init; }

    /// <summary>
    ///     变量类型注解，可为 <c>null</c> 表示类型推断
    /// </summary>
    public TypeNode? VarType { get; init; }

    /// <summary>
    ///     初始化表达式，可为 <c>null</c>
    /// </summary>
    public ValkyrieNode? Initializer { get; init; }

    /// <summary>
    ///     修饰符列表
    /// </summary>
    public IReadOnlyList<string> Modifiers { get; init; } = [];

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];
}
