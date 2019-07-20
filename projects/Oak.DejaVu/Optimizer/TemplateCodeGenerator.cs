using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Oak.DejaVu.Expressions;
using Oak.DejaVu.Security;

namespace Oak.DejaVu.Optimizer;

/// <summary>
///     模板代码生成器——将 AST 编译为可缓存的渲染委托。
///     使用 System.Linq.Expressions 构建表达式树，JIT 编译为原生代码。
/// </summary>
public sealed class TemplateCodeGenerator
{
    /// <summary>
    ///     将优化后的模板节点编译为渲染委托
    /// </summary>
    /// <param name="nodes">优化后的模板节点列表</param>
    /// <returns>渲染委托：输入上下文变量 → 输出渲染字符串</returns>
    public Func<IDictionary<string, object>, string> Compile(IReadOnlyList<DejaVuTemplateNode> nodes)
    {
        var contextParam = Expression.Parameter(typeof(IDictionary<string, object>), "ctx");
        var sbVar = Expression.Variable(typeof(StringBuilder), "sb");

        var expressions = new List<Expression>
        {
            Expression.Assign(sbVar, Expression.New(typeof(StringBuilder)))
        };

        foreach (var node in nodes)
        {
            expressions.Add(GenerateNode(node, contextParam, sbVar));
        }

        expressions.Add(Expression.Call(
            sbVar,
            typeof(StringBuilder).GetMethod("ToString", Type.EmptyTypes)!
        ));

        var body = Expression.Block(
            [sbVar],
            expressions
        );

        var lambda = Expression.Lambda<Func<IDictionary<string, object>, string>>(
            body,
            contextParam
        );

        return lambda.Compile();
    }

    private Expression GenerateNode(DejaVuTemplateNode node, ParameterExpression ctx, Expression sb)
    {
        return node switch
        {
            DejaVuTextNode textNode => GenerateTextNode(textNode, sb),
            DejaVuCodeNode codeNode => GenerateCodeNode(codeNode, ctx, sb),
            DejaVuIfNode ifNode => GenerateIfNode(ifNode, ctx, sb),
            DejaVuLoopNode loopNode => GenerateLoopNode(loopNode, ctx, sb),
            DejaVuLetNode letNode => GenerateLetNode(letNode, ctx, sb),
            DejaVuRawNode rawNode => GenerateRawNode(rawNode, ctx, sb),
            _ => Expression.Empty()
        };
    }

    private Expression GenerateTextNode(DejaVuTextNode textNode, Expression sb)
    {
        return Expression.Call(
            sb,
            typeof(StringBuilder).GetMethod("Append", [typeof(string)])!,
            Expression.Constant(textNode.Text)
        );
    }

    private Expression GenerateCodeNode(DejaVuCodeNode codeNode, ParameterExpression ctx, Expression sb)
    {
        var evalExpr = GenerateExpressionEval(codeNode.ParsedExpression, codeNode.Code, ctx);
        var escapedExpr = Expression.Call(
            typeof(HtmlEscaper).GetMethod("EscapeHtmlContent", [typeof(string)])!,
            evalExpr
        );

        return Expression.Call(
            sb,
            typeof(StringBuilder).GetMethod("Append", [typeof(string)])!,
            escapedExpr
        );
    }

    private Expression GenerateIfNode(DejaVuIfNode ifNode, ParameterExpression ctx, Expression sb)
    {
        var conditionExpr = GenerateExpressionEval(ifNode.ParsedCondition, ifNode.Condition, ctx);
        var toBoolCall = Expression.Call(
            typeof(TemplateCodeGenerator).GetMethod(nameof(ToBoolean), BindingFlags.NonPublic | BindingFlags.Static)!,
            conditionExpr
        );

        var thenExpr = GenerateNodesBlock(ifNode.Children, ctx, sb);
        var elseExpr = ifNode.ElseChildren.Count > 0
            ? GenerateNodesBlock(ifNode.ElseChildren, ctx, sb)
            : Expression.Empty();

        Expression result = Expression.IfThenElse(toBoolCall, thenExpr, elseExpr);

        foreach (var elseIfNode in ifNode.ElseIfNodes.AsEnumerable().Reverse())
        {
            var elseIfCondition = GenerateExpressionEval(elseIfNode.ParsedCondition, elseIfNode.Condition, ctx);
            var elseIfBool = Expression.Call(
                typeof(TemplateCodeGenerator).GetMethod(nameof(ToBoolean), BindingFlags.NonPublic | BindingFlags.Static)!,
                elseIfCondition
            );
            var elseIfBody = GenerateNodesBlock(elseIfNode.Children, ctx, sb);
            result = Expression.IfThenElse(elseIfBool, elseIfBody, result);
        }

        return result;
    }

    private Expression GenerateLoopNode(DejaVuLoopNode loopNode, ParameterExpression ctx, Expression sb)
    {
        var itemName = loopNode.ItemName ?? "item";
        var iterableExpr = GenerateExpressionEval(loopNode.ParsedExpression, loopNode.Expression, ctx);

        return Expression.Call(
            typeof(TemplateCodeGenerator).GetMethod(nameof(ExecuteLoop), BindingFlags.NonPublic | BindingFlags.Static)!,
            ctx,
            iterableExpr,
            Expression.Constant(itemName),
            Expression.Constant(loopNode.Children.ToArray()),
            Expression.Constant(this)
        );
    }

    private Expression GenerateLetNode(DejaVuLetNode letNode, ParameterExpression ctx, Expression sb)
    {
        var valueExpr = GenerateExpressionEval(letNode.ParsedExpression, letNode.Expression, ctx);

        return Expression.Call(
            typeof(TemplateCodeGenerator).GetMethod(nameof(ExecuteLet), BindingFlags.NonPublic | BindingFlags.Static)!,
            ctx,
            Expression.Constant(letNode.VariableName),
            valueExpr,
            Expression.Constant(letNode.Children.ToArray()),
            Expression.Constant(this)
        );
    }

    private Expression GenerateRawNode(DejaVuRawNode rawNode, ParameterExpression ctx, Expression sb)
    {
        return Expression.Call(
            typeof(TemplateCodeGenerator).GetMethod(nameof(ExecuteRaw), BindingFlags.NonPublic | BindingFlags.Static)!,
            ctx,
            Expression.Constant(rawNode.Children.ToArray()),
            Expression.Constant(this)
        );
    }

    private Expression GenerateNodesBlock(IReadOnlyList<DejaVuTemplateNode> nodes, ParameterExpression ctx, Expression sb)
    {
        if (nodes.Count == 0)
        {
            return Expression.Empty();
        }

        var expressions = new List<Expression>();
        foreach (var node in nodes)
        {
            expressions.Add(GenerateNode(node, ctx, sb));
        }

        return Expression.Block(expressions);
    }

    private Expression GenerateExpressionEval(IExpressionNode? parsedAst, string fallbackExpression, ParameterExpression ctx)
    {
        if (parsedAst != null)
        {
            return Expression.Call(
                typeof(TemplateCodeGenerator).GetMethod(nameof(EvaluateNode), BindingFlags.NonPublic | BindingFlags.Static)!,
                Expression.Constant(parsedAst),
                ctx
            );
        }

        return Expression.Call(
            typeof(TemplateCodeGenerator).GetMethod(nameof(EvaluateFallback), BindingFlags.NonPublic | BindingFlags.Static)!,
            Expression.Constant(fallbackExpression),
            ctx
        );
    }

    #region 运行时辅助方法

    private static bool ToBoolean(object? value)
    {
        if (value is bool b) return b;
        if (value is null) return false;
        return true;
    }

    private static void ExecuteLoop(
        IDictionary<string, object> ctx,
        object? iterable,
        string itemName,
        DejaVuTemplateNode[] children,
        TemplateCodeGenerator generator)
    {
        if (iterable is not System.Collections.IEnumerable enumerable) return;

        var sb = new StringBuilder();
        var index = 0;
        foreach (var item in enumerable)
        {
            var loopCtx = new Dictionary<string, object>(ctx)
            {
                [itemName] = item,
                ["index"] = index
            };

            var renderFunc = generator.Compile(children);
            sb.Append(renderFunc(loopCtx));
            index++;
        }
    }

    private static void ExecuteLet(
        IDictionary<string, object> ctx,
        string variableName,
        object? value,
        DejaVuTemplateNode[] children,
        TemplateCodeGenerator generator)
    {
        var letCtx = new Dictionary<string, object>(ctx)
        {
            [variableName] = value!
        };

        var renderFunc = generator.Compile(children);
        var sb = new StringBuilder();
        sb.Append(renderFunc(letCtx));
    }

    private static void ExecuteRaw(
        IDictionary<string, object> ctx,
        DejaVuTemplateNode[] children,
        TemplateCodeGenerator generator)
    {
        var renderFunc = generator.Compile(children);
        var sb = new StringBuilder();
        sb.Append(renderFunc(ctx));
    }

    private static object? EvaluateNode(IExpressionNode node, IDictionary<string, object> ctx)
    {
        var evaluator = new ExpressionEvaluator(ctx.ToDictionary(k => k.Key, k => (object?)k.Value));
        return evaluator.Evaluate(node);
    }

    private static object? EvaluateFallback(string expression, IDictionary<string, object> ctx)
    {
        var parser = new ExpressionParser();
        var ast = parser.Parse(expression);
        var evaluator = new ExpressionEvaluator(ctx.ToDictionary(k => k.Key, k => (object?)k.Value));
        return evaluator.Evaluate(ast);
    }

    #endregion
}
