using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Shader;

/// <summary>
///     着色器顶点属性声明，定义顶点缓冲区的布局
/// </summary>
/// <para>示例：</para>
/// <code>
/// @attribute(0) var position: vec3;
/// @attribute(1) var normal: vec3;
/// @attribute(2) var uv: vec2;
/// </code>
public sealed record ShaderAttributeDecl : ValkyrieNode
{
    /// <summary>
    ///     属性变量名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     属性数据类型
    /// </summary>
    public TypeNode AttrType { get; init; } = new();

    /// <summary>
    ///     属性在顶点缓冲区中的位置索引
    /// </summary>
    public int? Location { get; init; }

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];
}
