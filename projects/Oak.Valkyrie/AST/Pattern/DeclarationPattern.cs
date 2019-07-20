using Oak.Syntax;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Pattern;

/// <summary>
///     联合类型常量 —— 将匹配值绑定到变量
/// </summary>
/// <para>示例：</para>
/// <code>
/// match value {
///     n => { ... }          // Name = "n", TypeAnnotation = null
///     v: i32 => { ... }     // Name = "v", TypeAnnotation = TypeAnnotation("i32")
/// }
/// </code>
public sealed record DeclarationPattern : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public DeclarationPattern() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public DeclarationPattern(string name, TypeNode? typeAnnotation, TextSpan span)
    {
        Name = name;
        TypeAnnotation = typeAnnotation;
        Span = span;
    }

    /// <summary>
    ///     绑定变量名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     可选的类型标注
    /// </summary>
    public TypeNode? TypeAnnotation { get; init; }
}
