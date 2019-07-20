using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.ECS;

/// <summary>
///     组件声明，ECS 架构中的数据载体
/// </summary>
/// <para>示例：</para>
/// <code>
/// component Position {
///     x: f32;
///     y: f32;
///     z: f32;
/// }
/// </code>
public sealed record DeclareComponent : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     组件名称
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? Body { get; init; } = null;
}
