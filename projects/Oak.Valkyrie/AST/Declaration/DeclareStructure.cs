using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     结构体声明，值类型的数据聚合
/// </summary>
/// <para>示例：</para>
/// <code>
/// structure Point {
///     x: f32;
///     y: f32;
/// }
/// </code>
public sealed record DeclareStructure : ValkyrieNode, IDeclarationNode
{
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     结构体名称
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? Body { get; init; } = null;
}
