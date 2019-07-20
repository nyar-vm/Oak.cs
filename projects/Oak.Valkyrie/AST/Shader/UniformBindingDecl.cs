using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Shader;

/// <summary>
///     Uniform 绑定声明，将 CPU 侧的字段映射到 GPU 的 Uniform 绑定点
/// </summary>
/// <para>示例：</para>
/// <code>
/// @uniform_binding var sceneData: SceneUniforms @group(0) @binding(0);
/// </code>
public sealed record UniformBindingDecl : ValkyrieNode
{
    /// <summary>
    ///     绑定变量名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     绑定数据类型
    /// </summary>
    public TypeNode BindingType { get; init; } = new();

    /// <summary>
    ///     绑定组索引
    /// </summary>
    public int? Group { get; init; }

    /// <summary>
    ///     绑定槽索引
    /// </summary>
    public int? Binding { get; init; }

    /// <summary>
    ///     修饰符列表
    /// </summary>
    public IReadOnlyList<string> Modifiers { get; init; } = [];

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];
}
