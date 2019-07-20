using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Shader;

/// <summary>
///     Varying 变量声明，用于在顶点和片元着色器之间传递插值数据
/// </summary>
/// <para>示例：</para>
/// <code>
/// @varying var worldNormal: vec3;
/// @varying(linear) var texCoord: vec2;
/// </code>
public sealed record VaryingDecl : ValkyrieNode
{
    /// <summary>
    ///     Varying 变量名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     Varying 数据类型
    /// </summary>
    public TypeNode VaryingType { get; init; } = new();

    /// <summary>
    ///     插值方式（如 <c>"linear"</c>、<c>"perspective"</c>），为 <c>null</c> 时使用默认插值
    /// </summary>
    public string? Interpolation { get; init; }

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];
}
