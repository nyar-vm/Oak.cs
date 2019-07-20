using Oak.DejaVu.Expressions;
using Oak.DejaVu.Filters;
using Oak.Diagnostics;

namespace Oak.DejaVu.Optimizer;

/// <summary>
///     编译期类型检查器——变量类型推导、未定义变量检测、属性访问验证、过滤器类型匹配。
/// </summary>
public sealed class TypeChecker
{
    private readonly DiagnosticSink _diagnostics;
    private readonly FilterRegistry? _filters;

    /// <summary>
    ///     创建类型检查器
    /// </summary>
    /// <param name="diagnostics">诊断消息收集器</param>
    /// <param name="filters">过滤器注册表（用于过滤器类型检查）</param>
    public TypeChecker(DiagnosticSink diagnostics, FilterRegistry? filters = null)
    {
        _diagnostics = diagnostics;
        _filters = filters;
    }

    /// <summary>
    ///     对模板节点执行类型检查
    /// </summary>
    /// <param name="nodes">优化后的模板节点</param>
    /// <param name="knownTypes">已知变量类型（从外部 Data 类型注解提供）</param>
    /// <returns>推导出的变量类型表</returns>
    public Dictionary<string, TemplateType> Check(IReadOnlyList<DejaVuTemplateNode> nodes, Dictionary<string, TemplateType>? knownTypes = null)
    {
        var typeEnv = new TypeEnvironment(knownTypes);

        CheckNodes(nodes, typeEnv);

        return typeEnv.InferredTypes;
    }

    private void CheckNodes(IReadOnlyList<DejaVuTemplateNode> nodes, TypeEnvironment env)
    {
        foreach (var node in nodes)
        {
            CheckNode(node, env);
        }
    }

    private void CheckNode(DejaVuTemplateNode node, TypeEnvironment env)
    {
        switch (node)
        {
            case DejaVuCodeNode codeNode:
                CheckExpression(codeNode.ParsedExpression, codeNode.Code, env);
                break;
            case DejaVuIfNode ifNode:
                var condType = CheckExpression(ifNode.ParsedCondition, ifNode.Condition, env);
                if (condType != TemplateType.Unknown && condType != TemplateType.Boolean)
                {
                    _diagnostics.AddWarning(string.Empty, default, "NonBooleanCondition",
                        $"if 条件表达式类型为 {condType}，期望 Boolean");
                }

                CheckNodes(ifNode.Children, env);
                foreach (var elseIf in ifNode.ElseIfNodes)
                {
                    CheckExpression(elseIf.ParsedCondition, elseIf.Condition, env);
                    CheckNodes(elseIf.Children, env);
                }

                CheckNodes(ifNode.ElseChildren, env);
                break;
            case DejaVuLoopNode loopNode:
                var iterType = CheckExpression(loopNode.ParsedExpression, loopNode.Expression, env);
                if (iterType != TemplateType.Unknown && iterType != TemplateType.Array && iterType != TemplateType.Object)
                {
                    _diagnostics.AddWarning(string.Empty, default, "NonIterableLoop",
                        $"loop 表达式类型为 {iterType}，期望 Array 或 Object");
                }

                var itemName = loopNode.ItemName ?? "item";
                var itemType = iterType == TemplateType.Array ? TemplateType.Any : TemplateType.Unknown;
                env.PushScope();
                env.Declare(itemName, itemType);
                env.Declare("index", TemplateType.Number);
                CheckNodes(loopNode.Children, env);
                env.PopScope();
                break;
            case DejaVuLetNode letNode:
                var letType = CheckExpression(letNode.ParsedExpression, letNode.Expression, env);
                env.PushScope();
                env.Declare(letNode.VariableName, letType);
                CheckNodes(letNode.Children, env);
                env.PopScope();
                break;
            case DejaVuWithNode withNode:
                CheckExpression(withNode.ParsedExpression, withNode.Expression, env);
                env.PushScope();
                if (!string.IsNullOrEmpty(withNode.AliasName))
                {
                    env.Declare(withNode.AliasName, TemplateType.Object);
                }

                CheckNodes(withNode.Children, env);
                env.PopScope();
                break;
            case DejaVuBlockNode blockNode:
                CheckNodes(blockNode.Children, env);
                break;
            case DejaVuMatchNode matchNode:
                CheckExpression(matchNode.ParsedExpression, matchNode.Expression, env);
                CheckNodes(matchNode.Children, env);
                break;
            case DejaVuRawNode rawNode:
                CheckNodes(rawNode.Children, env);
                break;
        }
    }

    private TemplateType CheckExpression(IExpressionNode? parsedAst, string fallback, TypeEnvironment env)
    {
        if (parsedAst != null)
        {
            return CheckExpressionNode(parsedAst, env);
        }

        return TemplateType.Unknown;
    }

    private TemplateType CheckExpressionNode(IExpressionNode node, TypeEnvironment env)
    {
        return node switch
        {
            LiteralNode lit => CheckLiteral(lit),
            IdentifierNode id => CheckIdentifier(id, env),
            BinaryNode binary => CheckBinary(binary, env),
            UnaryNode unary => CheckUnary(unary, env),
            MemberAccessNode member => CheckMemberAccess(member, env),
            CallNode call => CheckCall(call, env),
            IndexNode index => CheckIndex(index, env),
            PipeNode pipe => CheckPipe(pipe, env),
            _ => TemplateType.Unknown
        };
    }

    private TemplateType CheckLiteral(LiteralNode lit)
    {
        return lit.Value switch
        {
            null => TemplateType.Null,
            bool => TemplateType.Boolean,
            double => TemplateType.Number,
            string => TemplateType.String,
            _ => TemplateType.Unknown
        };
    }

    private TemplateType CheckIdentifier(IdentifierNode id, TypeEnvironment env)
    {
        if (env.TryGetType(id.Name, out var type))
        {
            return type;
        }

        _diagnostics.AddWarning(string.Empty, default, "UndefinedVariable",
            $"未定义的变量 \"{id.Name}\"，运行期将从模板上下文中解析");

        env.InferType(id.Name, TemplateType.Any);
        return TemplateType.Any;
    }

    private TemplateType CheckBinary(BinaryNode binary, TypeEnvironment env)
    {
        var leftType = CheckExpressionNode(binary.Left, env);
        var rightType = CheckExpressionNode(binary.Right, env);

        return binary.Operator switch
        {
            BinaryOperator.Add => InferAddType(leftType, rightType),
            BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Modulo
                => TemplateType.Number,
            BinaryOperator.Equal or BinaryOperator.NotEqual => TemplateType.Boolean,
            BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual or BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual
                => TemplateType.Boolean,
            BinaryOperator.And or BinaryOperator.Or => TemplateType.Boolean,
            _ => TemplateType.Unknown
        };
    }

    private TemplateType CheckUnary(UnaryNode unary, TypeEnvironment env)
    {
        var operandType = CheckExpressionNode(unary.Operand, env);

        return unary.Operator switch
        {
            UnaryOperator.Negate => operandType == TemplateType.Number ? TemplateType.Number : TemplateType.Unknown,
            UnaryOperator.Not => TemplateType.Boolean,
            _ => TemplateType.Unknown
        };
    }

    private TemplateType CheckMemberAccess(MemberAccessNode member, TypeEnvironment env)
    {
        var objType = CheckExpressionNode(member.Object, env);

        if (objType is TemplateType.Unknown or TemplateType.Any)
        {
            return TemplateType.Any;
        }

        return TemplateType.Any;
    }

    private TemplateType CheckCall(CallNode call, TypeEnvironment env)
    {
        CheckExpressionNode(call.Function, env);
        foreach (var arg in call.Arguments)
        {
            CheckExpressionNode(arg, env);
        }

        return TemplateType.Any;
    }

    private TemplateType CheckIndex(IndexNode index, TypeEnvironment env)
    {
        var objType = CheckExpressionNode(index.Object, env);
        CheckExpressionNode(index.Index, env);

        if (objType == TemplateType.Array)
        {
            return TemplateType.Any;
        }

        return TemplateType.Any;
    }

    private TemplateType CheckPipe(PipeNode pipe, TypeEnvironment env)
    {
        var inputType = CheckExpressionNode(pipe.Left, env);
        foreach (var arg in pipe.Arguments)
        {
            CheckExpressionNode(arg, env);
        }

        if (_filters != null && !_filters.HasFilter(pipe.FilterName))
        {
            _diagnostics.AddWarning(string.Empty, default, "UnknownFilter",
                $"未知的过滤器 \"{pipe.FilterName}\"");
        }

        return InferFilterOutputType(pipe.FilterName, inputType);
    }

    private static TemplateType InferAddType(TemplateType left, TemplateType right)
    {
        if (left == TemplateType.String || right == TemplateType.String)
        {
            return TemplateType.String;
        }

        if (left == TemplateType.Number && right == TemplateType.Number)
        {
            return TemplateType.Number;
        }

        return TemplateType.Unknown;
    }

    private static TemplateType InferFilterOutputType(string filterName, TemplateType inputType)
    {
        return filterName switch
        {
            "uppercase" or "lowercase" or "trim" or "capitalize" or "strip_html" or "escape" or "newline_to_br"
                => TemplateType.String,
            "truncate" => TemplateType.String,
            "length" => TemplateType.Number,
            "first" or "last" => TemplateType.Any,
            "reverse" or "sort" => inputType == TemplateType.Array ? TemplateType.Array : TemplateType.String,
            "join" => TemplateType.String,
            "default" => inputType,
            _ => TemplateType.Any
        };
    }
}

/// <summary>
///     模板类型——编译期类型系统的类型枚举
/// </summary>
public enum TemplateType
{
    /// <summary>
    ///     未知类型（无法推导）
    /// </summary>
    Unknown,

    /// <summary>
    ///     任意类型（动态）
    /// </summary>
    Any,

    /// <summary>
    ///     空值
    /// </summary>
    Null,

    /// <summary>
    ///     布尔类型
    /// </summary>
    Boolean,

    /// <summary>
    ///     数字类型
    /// </summary>
    Number,

    /// <summary>
    ///     字符串类型
    /// </summary>
    String,

    /// <summary>
    ///     数组类型
    /// </summary>
    Array,

    /// <summary>
    ///     对象类型
    /// </summary>
    Object
}

/// <summary>
///     类型环境——变量作用域 + 类型绑定
/// </summary>
public sealed class TypeEnvironment
{
    private readonly Stack<Dictionary<string, TemplateType>> _scopes = new();
    private readonly Dictionary<string, TemplateType> _inferredTypes = new();

    /// <summary>
    ///     推导出的变量类型表
    /// </summary>
    public Dictionary<string, TemplateType> InferredTypes => _inferredTypes;

    /// <summary>
    ///     创建类型环境
    /// </summary>
    public TypeEnvironment(Dictionary<string, TemplateType>? knownTypes)
    {
        var globalScope = new Dictionary<string, TemplateType>();
        if (knownTypes != null)
        {
            foreach (var (name, type) in knownTypes)
            {
                globalScope[name] = type;
                _inferredTypes[name] = type;
            }
        }

        _scopes.Push(globalScope);
    }

    /// <summary>
    ///     声明变量类型
    /// </summary>
    public void Declare(string name, TemplateType type)
    {
        _scopes.Peek()[name] = type;
        _inferredTypes[name] = type;
    }

    /// <summary>
    ///     推导变量类型（仅在未声明时设置）
    /// </summary>
    public void InferType(string name, TemplateType type)
    {
        if (!_inferredTypes.ContainsKey(name))
        {
            _inferredTypes[name] = type;
        }
    }

    /// <summary>
    ///     查找变量类型
    /// </summary>
    public bool TryGetType(string name, out TemplateType type)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out type))
            {
                return true;
            }
        }

        type = TemplateType.Unknown;
        return false;
    }

    /// <summary>
    ///     推入新作用域
    /// </summary>
    public void PushScope()
    {
        _scopes.Push(new Dictionary<string, TemplateType>());
    }

    /// <summary>
    ///     弹出作用域
    /// </summary>
    public void PopScope()
    {
        if (_scopes.Count > 1)
        {
            _scopes.Pop();
        }
    }
}
