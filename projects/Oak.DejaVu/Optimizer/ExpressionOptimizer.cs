using Oak.DejaVu.Expressions;

namespace Oak.DejaVu.Optimizer;

/// <summary>
///     表达式优化器——常量折叠、表达式简化
/// </summary>
public static class ExpressionOptimizer
{
    /// <summary>
    ///     优化表达式 AST，执行常量折叠等变换
    /// </summary>
    public static IExpressionNode Optimize(IExpressionNode node)
    {
        return node switch
        {
            BinaryNode binary => OptimizeBinary(binary),
            UnaryNode unary => OptimizeUnary(unary),
            PipeNode pipe => OptimizePipe(pipe),
            MemberAccessNode member => OptimizeMemberAccess(member),
            CallNode call => OptimizeCall(call),
            IndexNode index => OptimizeIndex(index),
            _ => node
        };
    }

    /// <summary>
    ///     优化二元表达式——常量折叠
    /// </summary>
    private static IExpressionNode OptimizeBinary(BinaryNode binary)
    {
        var left = Optimize(binary.Left);
        var right = Optimize(binary.Right);

        if (left is LiteralNode leftLit && right is LiteralNode rightLit)
        {
            var result = EvaluateConstantBinary(leftLit.Value, rightLit.Value, binary.Operator);
            if (result != null)
            {
                return new LiteralNode { Value = result };
            }
        }

        if (ReferenceEquals(left, binary.Left) && ReferenceEquals(right, binary.Right))
        {
            return binary;
        }

        return new BinaryNode { Operator = binary.Operator, Left = left, Right = right };
    }

    /// <summary>
    ///     优化一元表达式——常量折叠
    /// </summary>
    private static IExpressionNode OptimizeUnary(UnaryNode unary)
    {
        var operand = Optimize(unary.Operand);

        if (operand is LiteralNode lit)
        {
            var result = EvaluateConstantUnary(lit.Value, unary.Operator);
            if (result != null)
            {
                return new LiteralNode { Value = result };
            }
        }

        if (ReferenceEquals(operand, unary.Operand))
        {
            return unary;
        }

        return new UnaryNode { Operator = unary.Operator, Operand = operand };
    }

    /// <summary>
    ///     优化管道表达式
    /// </summary>
    private static IExpressionNode OptimizePipe(PipeNode pipe)
    {
        var left = Optimize(pipe.Left);
        var args = pipe.Arguments.Select(Optimize).ToList();

        if (ReferenceEquals(left, pipe.Left) && args.SequenceEqual(pipe.Arguments))
        {
            return pipe;
        }

        return new PipeNode { Left = left, FilterName = pipe.FilterName, Arguments = args };
    }

    /// <summary>
    ///     优化成员访问表达式
    /// </summary>
    private static IExpressionNode OptimizeMemberAccess(MemberAccessNode member)
    {
        var obj = Optimize(member.Object);

        if (ReferenceEquals(obj, member.Object))
        {
            return member;
        }

        return new MemberAccessNode { Object = obj, MemberName = member.MemberName };
    }

    /// <summary>
    ///     优化函数调用表达式
    /// </summary>
    private static IExpressionNode OptimizeCall(CallNode call)
    {
        var function = Optimize(call.Function);
        var args = call.Arguments.Select(Optimize).ToList();

        if (ReferenceEquals(function, call.Function) && args.SequenceEqual(call.Arguments))
        {
            return call;
        }

        return new CallNode { Function = function, Arguments = args };
    }

    /// <summary>
    ///     优化索引表达式
    /// </summary>
    private static IExpressionNode OptimizeIndex(IndexNode index)
    {
        var obj = Optimize(index.Object);
        var idx = Optimize(index.Index);

        if (ReferenceEquals(obj, index.Object) && ReferenceEquals(idx, index.Index))
        {
            return index;
        }

        return new IndexNode { Object = obj, Index = idx };
    }

    /// <summary>
    ///     常量二元运算求值
    /// </summary>
    private static object? EvaluateConstantBinary(object? left, object? right, BinaryOperator op)
    {
        if (left == null || right == null)
        {
            return null;
        }

        try
        {
            if (left is string sl && right is string sr)
            {
                return op switch
                {
                    BinaryOperator.Add => sl + sr,
                    BinaryOperator.Equal => sl == sr,
                    BinaryOperator.NotEqual => sl != sr,
                    _ => null
                };
            }

            if (left is bool bl && right is bool br)
            {
                return op switch
                {
                    BinaryOperator.And => bl && br,
                    BinaryOperator.Or => bl || br,
                    BinaryOperator.Equal => bl == br,
                    BinaryOperator.NotEqual => bl != br,
                    _ => null
                };
            }

            var dl = Convert.ToDouble(left);
            var dr = Convert.ToDouble(right);

            return op switch
            {
                BinaryOperator.Add => dl + dr,
                BinaryOperator.Subtract => dl - dr,
                BinaryOperator.Multiply => dl * dr,
                BinaryOperator.Divide => dr != 0 ? dl / dr : null,
                BinaryOperator.Modulo => dr != 0 ? dl % dr : null,
                BinaryOperator.Equal => Math.Abs(dl - dr) < double.Epsilon,
                BinaryOperator.NotEqual => Math.Abs(dl - dr) >= double.Epsilon,
                BinaryOperator.LessThan => dl < dr,
                BinaryOperator.LessThanOrEqual => dl <= dr,
                BinaryOperator.GreaterThan => dl > dr,
                BinaryOperator.GreaterThanOrEqual => dl >= dr,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     常量一元运算求值
    /// </summary>
    private static object? EvaluateConstantUnary(object? value, UnaryOperator op)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            if (value is bool b)
            {
                return op switch
                {
                    UnaryOperator.Not => !b,
                    _ => null
                };
            }

            var d = Convert.ToDouble(value);
            return op switch
            {
                UnaryOperator.Negate => -d,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}
