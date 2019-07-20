using Oak.Syntax;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     显式类型转换表达式 <c>expr as T</c> 或 <c>expr as? T</c>
///     强制将值转换为目标类型，不安全转换会产生编译错误
/// </summary>
public sealed record TermAsExpression : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermAsExpression() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermAsExpression(ValkyrieNode operand, TypeNode targetType, TextSpan span)
    {
        Operand = operand;
        TargetType = targetType;
        Span = span;
    }

    /// <summary>
    ///     被转换的表达式
    /// </summary>
    public ValkyrieNode Operand { get; init; } = default!;

    /// <summary>
    ///     目标类型注解
    /// </summary>
    public TypeNode TargetType { get; init; } = new();

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType Type => ValkyrieNodeType.CastExpr;
}
