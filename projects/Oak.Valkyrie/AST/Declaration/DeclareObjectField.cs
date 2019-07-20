using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     字段声明，结构体、类或组件中的成员变量定义
/// </summary>
/// <para>示例：</para>
/// <code>
/// name: utf8;
/// health: f32 = 100.0;
/// </code>
public sealed record DeclareObjectField : ValkyrieNode
{
    /// <summary>
    ///     字段名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     字段类型注解
    /// </summary>
    public TypeNode FieldType { get; init; } = new();

    /// <summary>
    ///     默认值表达式，可为 <c>null</c>
    /// </summary>
    public ValkyrieNode? DefaultValue { get; init; }

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];

    /// <summary>
    ///     文档注释列表
    /// </summary>
    public IReadOnlyList<DocumentComment> DocComments { get; init; } = [];
}
