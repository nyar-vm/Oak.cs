using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Pattern;

namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     类型测试表达式 <c>x is T</c>
///     返回 bool，表示值是否为指定类型
/// </summary>
public sealed record TermIsExpression : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermIsExpression() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermIsExpression(ValkyrieNode operand, PatternNode targetPatternNode, TextSpan span)
    {
        Operand = operand;
        TargetPatternNode = targetPatternNode;
        Span = span;
    }

    /// <summary>
    ///     被测试的表达式
    /// </summary>
    public ValkyrieNode Operand { get; init; } = default!;

    /// <summary>
    ///     目标类型模式
    /// </summary>
    public PatternNode TargetPatternNode { get; init; } = new();

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType Type => ValkyrieNodeType.IsExpr;
}
