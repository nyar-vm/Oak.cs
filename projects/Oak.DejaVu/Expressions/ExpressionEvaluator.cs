using System.Collections;
using Oak.DejaVu.Filters;

namespace Oak.DejaVu.Expressions;

/// <summary>
///     表达式求值器
/// </summary>
public sealed class ExpressionEvaluator
{
    private readonly Dictionary<string, object?> _variables;
    private readonly FilterRegistry? _filters;

    /// <summary>
    ///     创建表达式求值器
    /// </summary>
    /// <param name="variables">变量表</param>
    /// <param name="filters">过滤器注册表</param>
    public ExpressionEvaluator(Dictionary<string, object?>? variables = null, FilterRegistry? filters = null)
    {
        _variables = variables ?? new Dictionary<string, object?>();
        _filters = filters;
    }

    /// <summary>
    ///     设置变量
    /// </summary>
    public void SetVariable(string name, object? value)
    {
        _variables[name] = value;
    }

    /// <summary>
    ///     求值表达式
    /// </summary>
    public object? Evaluate(IExpressionNode node)
    {
        return node switch
        {
            LiteralNode literal => literal.Value,
            IdentifierNode identifier => GetVariable(identifier.Name),
            BinaryNode binary => EvaluateBinary(binary),
            UnaryNode unary => EvaluateUnary(unary),
            MemberAccessNode memberAccess => EvaluateMemberAccess(memberAccess),
            CallNode call => EvaluateCall(call),
            IndexNode index => EvaluateIndex(index),
            PipeNode pipe => EvaluatePipe(pipe),
            _ => null
        };
    }

    /// <summary>
    ///     求值二元表达式
    /// </summary>
    private object? EvaluateBinary(BinaryNode binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);

        return binary.Operator switch
        {
            BinaryOperator.Add => Add(left, right),
            BinaryOperator.Subtract => Subtract(left, right),
            BinaryOperator.Multiply => Multiply(left, right),
            BinaryOperator.Divide => Divide(left, right),
            BinaryOperator.Modulo => Modulo(left, right),
            BinaryOperator.Equal => Equal(left, right),
            BinaryOperator.NotEqual => !Equal(left, right),
            BinaryOperator.LessThan => Compare(left, right) < 0,
            BinaryOperator.LessThanOrEqual => Compare(left, right) <= 0,
            BinaryOperator.GreaterThan => Compare(left, right) > 0,
            BinaryOperator.GreaterThanOrEqual => Compare(left, right) >= 0,
            BinaryOperator.And => ToBoolean(left) && ToBoolean(right),
            BinaryOperator.Or => ToBoolean(left) || ToBoolean(right),
            _ => null
        };
    }

    /// <summary>
    ///     求值一元表达式
    /// </summary>
    private object? EvaluateUnary(UnaryNode unary)
    {
        var operand = Evaluate(unary.Operand);

        return unary.Operator switch
        {
            UnaryOperator.Negate => Negate(operand),
            UnaryOperator.Not => !ToBoolean(operand),
            _ => null
        };
    }

    /// <summary>
    ///     求值成员访问
    /// </summary>
    private object? EvaluateMemberAccess(MemberAccessNode memberAccess)
    {
        var obj = Evaluate(memberAccess.Object);
        if (obj == null) return null;

        if (obj is IDictionary dict)
        {
            return dict.Contains(memberAccess.MemberName) ? dict[memberAccess.MemberName] : null;
        }

        var type = obj.GetType();
        var property = type.GetProperty(memberAccess.MemberName);
        if (property != null) return property.GetValue(obj);

        var field = type.GetField(memberAccess.MemberName);
        if (field != null) return field.GetValue(obj);

        return null;
    }

    /// <summary>
    ///     求值函数调用
    /// </summary>
    private object? EvaluateCall(CallNode call)
    {
        var function = Evaluate(call.Function);
        var arguments = call.Arguments.Select(Evaluate).ToArray();

        if (function is Delegate delegateFunc) return delegateFunc.DynamicInvoke(arguments);

        return null;
    }

    /// <summary>
    ///     求值索引访问
    /// </summary>
    private object? EvaluateIndex(IndexNode index)
    {
        var obj = Evaluate(index.Object);
        var idx = Evaluate(index.Index);

        if (obj is IList list && idx is int i) return list[i];

        if (obj is IDictionary dict && idx != null) return dict[idx];

        return null;
    }

    /// <summary>
    ///     求值管道表达式
    /// </summary>
    private object? EvaluatePipe(PipeNode pipe)
    {
        var value = Evaluate(pipe.Left);

        if (_filters == null)
        {
            return value;
        }

        var args = pipe.Arguments.Select(Evaluate).ToArray();
        return _filters.Apply(pipe.FilterName, value, args);
    }

    private object? GetVariable(string name)
    {
        return _variables.TryGetValue(name, out var value) ? value : null;
    }

    private static object? Add(object? left, object? right)
    {
        if (left is string || right is string) return $"{left}{right}";
        if (left is double d1 && right is double d2) return d1 + d2;
        return null;
    }

    private static object? Subtract(object? left, object? right)
    {
        if (left is double d1 && right is double d2) return d1 - d2;
        return null;
    }

    private static object? Multiply(object? left, object? right)
    {
        if (left is double d1 && right is double d2) return d1 * d2;
        return null;
    }

    private static object? Divide(object? left, object? right)
    {
        if (left is double d1 && right is double d2 && d2 != 0) return d1 / d2;
        return null;
    }

    private static object? Modulo(object? left, object? right)
    {
        if (left is double d1 && right is double d2 && d2 != 0) return d1 % d2;
        return null;
    }

    private static bool Equal(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        return left.Equals(right);
    }

    private static int Compare(object? left, object? right)
    {
        if (left is double d1 && right is double d2) return d1.CompareTo(d2);
        if (left is string s1 && right is string s2) return string.Compare(s1, s2, StringComparison.Ordinal);
        return 0;
    }

    private static bool ToBoolean(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is double d) return d != 0;
        if (value is string s) return !string.IsNullOrEmpty(s);
        return true;
    }

    private static object? Negate(object? value)
    {
        if (value is double d) return -d;
        return null;
    }
}