using Oak.Valkyrie.AST.Declaration;

namespace Oak.Valkyrie.AST.Shader;

/// <summary>
///     着色器阶段声明的抽象基类
/// </summary>
/// <para>子类型：<see cref="VertexShaderDecl"/>（顶点着色器）、<see cref="FragmentShaderDecl"/>（片元着色器）、<see cref="ComputeShaderDecl"/>（计算着色器）</para>
/// <para>每个阶段可以包含属性、varying、uniform 声明和入口函数</para>
public abstract record ShaderStageDecl : ValkyrieNode
{
    /// <summary>
    ///     阶段名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     阶段体内的节点列表（包含属性、varying、uniform 等声明）
    /// </summary>
    public IReadOnlyList<ValkyrieNode> Body { get; init; } = [];

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];
}
