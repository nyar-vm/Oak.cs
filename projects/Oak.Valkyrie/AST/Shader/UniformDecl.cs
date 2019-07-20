using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Shader;

/// <summary>
///     Shader Uniform 变量声明，定义从 CPU 传入 GPU 的常量数据
/// </summary>
/// <para>示例：</para>
/// <code>
/// @uniform var mvp: mat4 @group(0) @binding(0);
/// @uniform var lightPos: vec3 @group(1) @binding(0);
/// </code>
public sealed record UniformDecl : ValkyrieNode
{
    /// <summary>
    ///     Uniform 变量名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     Uniform 数据类型
    /// </summary>
    public TypeNode UniformType { get; init; } = new();

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
