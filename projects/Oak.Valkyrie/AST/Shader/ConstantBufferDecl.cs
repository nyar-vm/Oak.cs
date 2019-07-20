using Oak.Valkyrie.AST.Declaration;

namespace Oak.Valkyrie.AST.Shader;

/// <summary>
///     常量缓冲区声明，用于 Shader 中的 Uniform 常量数据定义
/// </summary>
/// <para>示例：</para>
/// <code>
/// cbuffer CameraData @group(0) @binding(0) {
///     var viewMatrix: mat4;
///     var projMatrix: mat4;
/// }
/// </code>
public sealed record ConstantBufferDecl : ValkyrieNode
{
    /// <summary>
    ///     缓冲区名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     缓冲区字段列表
    /// </summary>
    public IReadOnlyList<DeclareObjectField> Fields { get; init; } = [];

    /// <summary>
    ///     绑定组（descriptor set）索引
    /// </summary>
    public int? Group { get; init; }

    /// <summary>
    ///     绑定槽索引
    /// </summary>
    public int? Binding { get; init; }

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];
}
