using Oak.Syntax;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Pattern;

/// <summary>
///     类型模式 —— 按类型匹配并可选绑定变量，用于 <c>match</c> 语句中
/// </summary>
/// <para>示例：</para>
/// <code>
/// match value {
///     i32 n => { print("整数 {n}"); }      // TypeAnnotation = TypeAnnotation("i32"), BindingName = "n"
///     utf8 => { print("字符串"); }          // TypeAnnotation = TypeAnnotation("utf8"), BindingName = null
///     f32 f => { print("浮点 {f}"); }
/// }
/// </code>
public sealed record PatternNode : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public PatternNode() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public PatternNode(TypeNode typeNode, string? bindingName, TextSpan span)
    {
        TypeNode = typeNode;
        BindingName = bindingName;
        Span = span;
    }

    /// <summary>
    ///     匹配的类型标注
    /// </summary>
    public TypeNode TypeNode { get; init; } = new();

    /// <summary>
    ///     可选的绑定变量名（为 <c>null</c> 时仅做类型检查不绑定）
    /// </summary>
    public string? BindingName { get; init; }
}
