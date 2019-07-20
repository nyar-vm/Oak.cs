using Oak.Valkyrie.AST.Declaration;

namespace Oak.Valkyrie.AST.Shader;

/// <summary>
///     Shader 采样器声明，定义纹理采样的参数配置
/// </summary>
/// <para>示例：</para>
/// <code>
/// @uniform var albedoSampler: sampler @group(1) @binding(1);
/// @uniform var shadowSampler: sampler @group(2) @binding(0);
/// </code>
public sealed record SamplerDecl : ValkyrieNode
{
    /// <summary>
    ///     采样器变量名
    /// </summary>
    public string Name { get; init; } = string.Empty;

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
