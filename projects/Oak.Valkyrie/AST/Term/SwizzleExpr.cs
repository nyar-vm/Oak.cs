using Oak.Syntax;

namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     Swizzle 分量访问表达式，用于向量分量的任意组合访问
/// </summary>
/// <para>示例：</para>
/// <code>
/// color.rgba          // Target = IdentifierNode("color"), Components = "rgba"
/// position.xy         // Target = IdentifierNode("position"), Components = "xy"
/// vec.xxyy            // Target = IdentifierNode("vec"), Components = "xxyy"
/// </code>
public sealed record SwizzleExpr : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public SwizzleExpr() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public SwizzleExpr(ValkyrieNode target, string? components, TextSpan span)
    {
        Target = target;
        Components = components ?? string.Empty;
        Span = span;
    }

    /// <summary>
    ///     目标向量表达式
    /// </summary>
    public ValkyrieNode Target { get; init; } = new IdentifierNode();

    /// <summary>
    ///     Swizzle 分量字符串（如 <c>"rgba"</c>、<c>"xyzw"</c>）
    /// </summary>
    public string Components { get; init; } = string.Empty;
}
