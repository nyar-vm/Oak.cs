using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Shader;

/// <summary>
///     Shader 纹理声明，定义着色器中使用的纹理资源绑定
/// </summary>
/// <para>示例：</para>
/// <code>
/// @uniform var albedoMap: texture2D @group(1) @binding(0);
/// @uniform var normalMap: texture2D @group(1) @binding(1);
/// </code>
public sealed record TextureDecl : ValkyrieNode
{
    /// <summary>
    ///     纹理变量名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     纹理类型（如 <c>"texture2D"</c>、<c>"textureCube"</c>）
    /// </summary>
    public TypeNode TextureType { get; init; } = new();

    /// <summary>
    ///     绑定组索引
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
