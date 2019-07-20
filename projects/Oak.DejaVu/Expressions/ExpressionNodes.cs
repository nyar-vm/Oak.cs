namespace Oak.DejaVu.Expressions;

/// <summary>
///     表达式节点接口
/// </summary>
public interface IExpressionNode
{
}

/// <summary>
///     字面量节点
/// </summary>
public sealed class LiteralNode : IExpressionNode
{
    /// <summary>
    ///     字面量值
    /// </summary>
    public object? Value { get; init; }
}

/// <summary>
///     标识符节点
/// </summary>
public sealed class IdentifierNode : IExpressionNode
{
    /// <summary>
    ///     标识符名称
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
///     二元运算符
/// </summary>
public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    And,
    Or
}

/// <summary>
///     二元节点
/// </summary>
public sealed class BinaryNode : IExpressionNode
{
    /// <summary>
    ///     运算符
    /// </summary>
    public BinaryOperator Operator { get; init; }

    /// <summary>
    ///     左操作数
    /// </summary>
    public IExpressionNode Left { get; init; } = null!;

    /// <summary>
    ///     右操作数
    /// </summary>
    public IExpressionNode Right { get; init; } = null!;
}

/// <summary>
///     一元运算符
/// </summary>
public enum UnaryOperator
{
    Negate,
    Not
}

/// <summary>
///     一元节点
/// </summary>
public sealed class UnaryNode : IExpressionNode
{
    /// <summary>
    ///     运算符
    /// </summary>
    public UnaryOperator Operator { get; init; }

    /// <summary>
    ///     操作数
    /// </summary>
    public IExpressionNode Operand { get; init; } = null!;
}

/// <summary>
///     成员访问节点
/// </summary>
public sealed class MemberAccessNode : IExpressionNode
{
    /// <summary>
    ///     目标对象
    /// </summary>
    public IExpressionNode Object { get; init; } = null!;

    /// <summary>
    ///     成员名称
    /// </summary>
    public string MemberName { get; init; } = string.Empty;
}

/// <summary>
///     函数调用节点
/// </summary>
public sealed class CallNode : IExpressionNode
{
    /// <summary>
    ///     被调用的函数
    /// </summary>
    public IExpressionNode Function { get; init; } = null!;

    /// <summary>
    ///     实参列表
    /// </summary>
    public List<IExpressionNode> Arguments { get; init; } = [];
}

/// <summary>
///     索引节点
/// </summary>
public sealed class IndexNode : IExpressionNode
{
    /// <summary>
    ///     被索引的对象
    /// </summary>
    public IExpressionNode Object { get; init; } = null!;

    /// <summary>
    ///     索引值
    /// </summary>
    public IExpressionNode Index { get; init; } = null!;
}

/// <summary>
///     管道节点
/// </summary>
public sealed class PipeNode : IExpressionNode
{
    /// <summary>
    ///     管道左侧表达式
    /// </summary>
    public IExpressionNode Left { get; init; } = null!;

    /// <summary>
    ///     过滤器名称
    /// </summary>
    public string FilterName { get; init; } = string.Empty;

    /// <summary>
    ///     过滤器参数
    /// </summary>
    public List<IExpressionNode> Arguments { get; init; } = [];
}