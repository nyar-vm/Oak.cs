namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     域声明，类内部的嵌套作用域，用于组织相关字段和函数
/// </summary>
/// <para>示例：</para>
/// <code>
/// class GameObject {
///     Physics {
///         var velocity: vec3;
///         var mass: f32;
///         fn apply_force(f: vec3) { ... }
///     }
/// }
/// </code>
public sealed record DeclareObjectDomain : ValkyrieNode
{
    /// <summary>
    ///     域名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody Body { get; init; } = new();
}
