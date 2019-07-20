using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Type;

/// <summary>
///     类型注解 —— 可表示简单类型、泛型、列表、数组、联合、交叉、函数等多种类型形态
/// </summary>
/// <para>支持的完整类型语法：</para>
/// <list type="bullet">
///     <item><c>i32</c>、<c>f32</c>、<c>utf8</c> — 基础简单类型</item>
///     <item><c>List&lt;i32&gt;</c> — 泛型类型</item>
///     <item><c>[i32]</c> — 列表/数组语法糖（<c>IsListType</c> / <c>IsArrayType</c>）</item>
///     <item><c>i32?</c> — 可空类型（<c>IsNullable</c>）</item>
///     <item><c>i32 | f32</c> — 联合类型（<c>IsUnionType</c>）</item>
///     <item><c>A &amp; B</c> — 交叉类型（<c>IsIntersectionType</c>）</item>
///     <item><c>micro(i32) -> bool</c> — 函数类型（<c>IsFunctionType</c>）</item>
/// </list>
/// <para>示例：</para>
/// <code>
/// var x: i32;                              // Name = "i32"
/// var list: List&lt;utf8&gt;;            // Name = "List", GenericArgs = [TypeAnnotation("utf8")]
/// var maybe: f32?;                       // IsNullable = true
/// var either: i32 | utf8;                // IsUnionType = true
/// var callback: fn(i32) -> bool;           // IsFunctionType = true
/// </code>
public record TypeNode : ValkyrieNode
{
    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<TypeNode> GenericArgs { get; init; } = [];

    public TypeNode() { }

    public TypeNode(string name, IReadOnlyList<TypeNode>? genericArgs = null)
    {
        Name = name;
        GenericArgs = genericArgs ?? [];
    }
}