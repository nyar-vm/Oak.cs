using Oak.Syntax;

namespace Oak.Valkyrie.AST.Pattern;

/// <summary>
///     或模式 <c>case 1 | 2 | 3:</c>
///     匹配任一子模式
/// </summary>
public sealed record PatternOrExpression : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public PatternOrExpression() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public PatternOrExpression(IReadOnlyList<ValkyrieNode> alternatives, TextSpan span)
    {
        Alternatives = alternatives;
        Span = span;
    }

    /// <summary>
    ///     备选模式列表
    /// </summary>
    public IReadOnlyList<ValkyrieNode> Alternatives { get; init; } = [];

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType Type => ValkyrieNodeType.OrPattern;
}
