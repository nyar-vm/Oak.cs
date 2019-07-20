using Oak.Syntax;
using Oak.Valkyrie.AST.Template;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     Widget UI 组件声明，用于声明式 UI 构建
/// </summary>
/// <para>示例：</para>
/// <code>
/// widget Button {
///     var text: utf8;
///     var onClick: fn() -> void;
///
///     fn render() -> View {
///         View {
///             Text { text: self.text }
///         }
///     }
/// }
/// </code>
public sealed record DeclareWidget : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     Widget 名称
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     Widget 属性列表（对外暴露的可配置项）
    /// </summary>
    public IReadOnlyList<DeclareObjectField> Properties { get; init; } = [];

    /// <summary>
    ///     渲染方法，为 <c>null</c> 时使用默认渲染
    /// </summary>
    public DeclareMicro? RenderMethod { get; init; }
}