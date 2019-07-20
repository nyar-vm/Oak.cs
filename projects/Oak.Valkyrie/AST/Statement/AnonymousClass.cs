using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Template;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     类声明节点
/// </summary>
/// <para>示例：</para>
/// <code>
/// call(class {
///     name: utf8;
///     health: f32 = 100.0;
///     heal(mut self, amount: f32) {
///         self.health += amount;
///     }
/// })
/// </code>
/// <para>支持泛型：</para>
/// <code>
/// class Container&lt;T&gt; where T : Serializable {
///     var data: T;
/// }
/// </code>
public sealed record AnonymousClass : ValkyrieNode
{
    /// <summary>
    ///     继承规范列表（基类、接口等）
    /// </summary>
    public IReadOnlyList<InheritanceList> Inheritances { get; init; } = [];


    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];

    /// <summary>
    ///     元信息声明列表
    /// </summary>
    public IReadOnlyList<ArmWhenFragment> Metas { get; init; } = [];

    /// <summary>
    ///     修饰符列表（如 <c>public</c>、<c>abstract</c>）
    /// </summary>
    public IReadOnlyList<string> Modifiers { get; init; } = [];

    /// <summary>
    ///     文档注释列表
    /// </summary>
    public IReadOnlyList<DocumentComment> DocComments { get; init; } = [];

    /// <summary>
    ///     泛型类型参数列表
    /// </summary>
    public IReadOnlyList<TypeParameter> TypeParameters { get; init; } = [];

    /// <summary>
    ///     泛型约束列表（<c>where</c> 子句）
    /// </summary>
    public IReadOnlyList<GenericConstraint> GenericConstraints { get; init; } = [];

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? Body { get; init; } = null;
}