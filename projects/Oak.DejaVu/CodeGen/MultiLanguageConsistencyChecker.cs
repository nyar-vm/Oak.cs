using System.Text;
using Oak.DejaVu.CodeGen;
using Oak.DejaVu.Optimizer;

namespace Oak.DejaVu.CodeGen;

/// <summary>
///     多语言一致性验证器——验证同一模板生成的 C#/TypeScript/Java 代码结构一致性。
///     通过比较 AST 节点覆盖率、过滤器覆盖率和表达式类型覆盖率来评估一致性。
/// </summary>
public sealed class MultiLanguageConsistencyChecker
{
    /// <summary>
    ///     一致性检查结果
    /// </summary>
    public sealed class ConsistencyResult
    {
        /// <summary>
        ///     模板源码
        /// </summary>
        public string TemplateSource { get; init; } = string.Empty;

        /// <summary>
        ///     C# 渲染输出
        /// </summary>
        public string CSharpOutput { get; init; } = string.Empty;

        /// <summary>
        ///     TypeScript 生成源码
        /// </summary>
        public string TypeScriptOutput { get; init; } = string.Empty;

        /// <summary>
        ///     Java 生成源码
        /// </summary>
        public string JavaOutput { get; init; } = string.Empty;

        /// <summary>
        ///     AST 节点覆盖率（0.0-1.0）
        /// </summary>
        public double AstCoverage { get; init; }

        /// <summary>
        ///     过滤器覆盖率（0.0-1.0）
        /// </summary>
        public double FilterCoverage { get; init; }

        /// <summary>
        ///     表达式类型覆盖率（0.0-1.0）
        /// </summary>
        public double ExpressionCoverage { get; init; }

        /// <summary>
        ///     总体一致性得分（0.0-1.0）
        /// </summary>
        public double OverallScore => (AstCoverage + FilterCoverage + ExpressionCoverage) / 3.0;

        /// <summary>
        ///     未覆盖的 AST 节点类型
        /// </summary>
        public List<string> UncoveredNodeTypes { get; init; } = [];

        /// <summary>
        ///     未覆盖的过滤器
        /// </summary>
        public List<string> UncoveredFilters { get; init; } = [];

        /// <summary>
        ///     未覆盖的表达式类型
        /// </summary>
        public List<string> UncoveredExpressionTypes { get; init; } = [];
    }

    /// <summary>
    ///     验证模板的多语言一致性
    /// </summary>
    /// <param name="source">模板源码</param>
    /// <param name="compiler">编译器</param>
    /// <returns>一致性检查结果</returns>
    public ConsistencyResult Check(string source, DejaVuCompiler compiler)
    {
        var parseResult = new DejaVuParser("doki").Parse(source);
        var optimizer = new TemplateOptimizer();
        var optimizedNodes = optimizer.Optimize(parseResult.Nodes.ToList());

        var csharpOutput = compiler.Compile(source).RenderFunc != null
            ? "JIT compiled"
            : "Interpreter mode";

        var tsOutput = compiler.CompileToTypeScript(source);
        var javaOutput = compiler.CompileToJava(source);

        var nodeTypes = CollectNodeTypes(optimizedNodes);
        var expressionTypes = CollectExpressionTypes(optimizedNodes);
        var filters = CollectFilters(optimizedNodes);

        var tsNodeCoverage = CheckNodeTypeCoverage(nodeTypes, tsOutput, "TypeScript");
        var javaNodeCoverage = CheckNodeTypeCoverage(nodeTypes, javaOutput, "Java");

        var tsExprCoverage = CheckExpressionCoverage(expressionTypes, tsOutput);
        var javaExprCoverage = CheckExpressionCoverage(expressionTypes, javaOutput);

        var tsFilterCoverage = CheckFilterCoverage(filters, tsOutput);
        var javaFilterCoverage = CheckFilterCoverage(filters, javaOutput);

        var astCoverage = (tsNodeCoverage + javaNodeCoverage) / 2.0;
        var filterCoverage = (tsFilterCoverage + javaFilterCoverage) / 2.0;
        var exprCoverage = (tsExprCoverage + javaExprCoverage) / 2.0;

        var uncoveredNodes = new List<string>();
        foreach (var nodeType in nodeTypes)
        {
            if (!tsOutput.Contains(nodeType, StringComparison.OrdinalIgnoreCase) &&
                !javaOutput.Contains(nodeType, StringComparison.OrdinalIgnoreCase))
            {
                uncoveredNodes.Add(nodeType);
            }
        }

        return new ConsistencyResult
        {
            TemplateSource = source,
            CSharpOutput = csharpOutput,
            TypeScriptOutput = tsOutput,
            JavaOutput = javaOutput,
            AstCoverage = astCoverage,
            FilterCoverage = filterCoverage,
            ExpressionCoverage = exprCoverage,
            UncoveredNodeTypes = uncoveredNodes,
            UncoveredFilters = filters.Where(f =>
                !tsOutput.Contains(f, StringComparison.OrdinalIgnoreCase) &&
                !javaOutput.Contains(f, StringComparison.OrdinalIgnoreCase)).ToList(),
            UncoveredExpressionTypes = expressionTypes.Where(e =>
                !tsOutput.Contains(e, StringComparison.OrdinalIgnoreCase) &&
                !javaOutput.Contains(e, StringComparison.OrdinalIgnoreCase)).ToList()
        };
    }

    private static HashSet<string> CollectNodeTypes(IReadOnlyList<DejaVuTemplateNode> nodes)
    {
        var types = new HashSet<string>();

        foreach (var node in nodes)
        {
            types.Add(node.NodeType.ToString());

            switch (node)
            {
                case DejaVuIfNode ifNode:
                    CollectNodeTypes(ifNode.Children);
                    foreach (var elseIf in ifNode.ElseIfNodes)
                    {
                        types.Add("ElseIf");
                        CollectNodeTypes(elseIf.Children);
                    }

                    CollectNodeTypes(ifNode.ElseChildren);
                    break;
                case DejaVuLoopNode loopNode:
                    CollectNodeTypes(loopNode.Children);
                    break;
                case DejaVuLetNode letNode:
                    CollectNodeTypes(letNode.Children);
                    break;
                case DejaVuWithNode withNode:
                    CollectNodeTypes(withNode.Children);
                    break;
                case DejaVuBlockNode blockNode:
                    CollectNodeTypes(blockNode.Children);
                    break;
                case DejaVuRawNode rawNode:
                    CollectNodeTypes(rawNode.Children);
                    break;
                case DejaVuMatchNode matchNode:
                    CollectNodeTypes(matchNode.Children);
                    break;
            }
        }

        return types;
    }

    private static HashSet<string> CollectExpressionTypes(IReadOnlyList<DejaVuTemplateNode> nodes)
    {
        var types = new HashSet<string>();

        foreach (var node in nodes)
        {
            switch (node)
            {
                case DejaVuCodeNode codeNode:
                    CollectExprTypes(codeNode.ParsedExpression, types);
                    break;
                case DejaVuIfNode ifNode:
                    CollectExprTypes(ifNode.ParsedCondition, types);
                    CollectExpressionTypes(ifNode.Children);
                    foreach (var elseIf in ifNode.ElseIfNodes)
                    {
                        CollectExprTypes(elseIf.ParsedCondition, types);
                        CollectExpressionTypes(elseIf.Children);
                    }

                    CollectExpressionTypes(ifNode.ElseChildren);
                    break;
                case DejaVuLoopNode loopNode:
                    CollectExprTypes(loopNode.ParsedExpression, types);
                    CollectExpressionTypes(loopNode.Children);
                    break;
                case DejaVuLetNode letNode:
                    CollectExprTypes(letNode.ParsedExpression, types);
                    CollectExpressionTypes(letNode.Children);
                    break;
                case DejaVuWithNode withNode:
                    CollectExprTypes(withNode.ParsedExpression, types);
                    CollectExpressionTypes(withNode.Children);
                    break;
                case DejaVuMatchNode matchNode:
                    CollectExprTypes(matchNode.ParsedExpression, types);
                    CollectExpressionTypes(matchNode.Children);
                    break;
                case DejaVuRawNode rawNode:
                    CollectExpressionTypes(rawNode.Children);
                    break;
            }
        }

        return types;
    }

    private static void CollectExprTypes(Expressions.IExpressionNode? node, HashSet<string> types)
    {
        if (node == null) return;
        types.Add(node.GetType().Name);

        switch (node)
        {
            case Expressions.BinaryNode binary:
                CollectExprTypes(binary.Left, types);
                CollectExprTypes(binary.Right, types);
                break;
            case Expressions.UnaryNode unary:
                CollectExprTypes(unary.Operand, types);
                break;
            case Expressions.MemberAccessNode member:
                CollectExprTypes(member.Object, types);
                break;
            case Expressions.CallNode call:
                CollectExprTypes(call.Function, types);
                foreach (var arg in call.Arguments) CollectExprTypes(arg, types);
                break;
            case Expressions.IndexNode index:
                CollectExprTypes(index.Object, types);
                CollectExprTypes(index.Index, types);
                break;
            case Expressions.PipeNode pipe:
                CollectExprTypes(pipe.Left, types);
                foreach (var arg in pipe.Arguments) CollectExprTypes(arg, types);
                break;
        }
    }

    private static HashSet<string> CollectFilters(IReadOnlyList<DejaVuTemplateNode> nodes)
    {
        var filters = new HashSet<string>();
        CollectFiltersFromNodes(nodes, filters);
        return filters;
    }

    private static void CollectFiltersFromNodes(IReadOnlyList<DejaVuTemplateNode> nodes, HashSet<string> filters)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case DejaVuCodeNode codeNode:
                    CollectFiltersFromExpr(codeNode.ParsedExpression, filters);
                    break;
                case DejaVuIfNode ifNode:
                    CollectFiltersFromExpr(ifNode.ParsedCondition, filters);
                    CollectFiltersFromNodes(ifNode.Children, filters);
                    foreach (var elseIf in ifNode.ElseIfNodes)
                    {
                        CollectFiltersFromExpr(elseIf.ParsedCondition, filters);
                        CollectFiltersFromNodes(elseIf.Children, filters);
                    }

                    CollectFiltersFromNodes(ifNode.ElseChildren, filters);
                    break;
                case DejaVuLoopNode loopNode:
                    CollectFiltersFromExpr(loopNode.ParsedExpression, filters);
                    CollectFiltersFromNodes(loopNode.Children, filters);
                    break;
                case DejaVuLetNode letNode:
                    CollectFiltersFromExpr(letNode.ParsedExpression, filters);
                    CollectFiltersFromNodes(letNode.Children, filters);
                    break;
                case DejaVuWithNode withNode:
                    CollectFiltersFromExpr(withNode.ParsedExpression, filters);
                    CollectFiltersFromNodes(withNode.Children, filters);
                    break;
                case DejaVuMatchNode matchNode:
                    CollectFiltersFromExpr(matchNode.ParsedExpression, filters);
                    CollectFiltersFromNodes(matchNode.Children, filters);
                    break;
                case DejaVuRawNode rawNode:
                    CollectFiltersFromNodes(rawNode.Children, filters);
                    break;
            }
        }
    }

    private static void CollectFiltersFromExpr(Expressions.IExpressionNode? node, HashSet<string> filters)
    {
        if (node == null) return;

        if (node is Expressions.PipeNode pipe)
        {
            filters.Add(pipe.FilterName);
            CollectFiltersFromExpr(pipe.Left, filters);
            foreach (var arg in pipe.Arguments) CollectFiltersFromExpr(arg, filters);
        }
        else if (node is Expressions.BinaryNode binary)
        {
            CollectFiltersFromExpr(binary.Left, filters);
            CollectFiltersFromExpr(binary.Right, filters);
        }
        else if (node is Expressions.UnaryNode unary)
        {
            CollectFiltersFromExpr(unary.Operand, filters);
        }
        else if (node is Expressions.MemberAccessNode member)
        {
            CollectFiltersFromExpr(member.Object, filters);
        }
        else if (node is Expressions.CallNode call)
        {
            CollectFiltersFromExpr(call.Function, filters);
            foreach (var arg in call.Arguments) CollectFiltersFromExpr(arg, filters);
        }
    }

    private static double CheckNodeTypeCoverage(HashSet<string> nodeTypes, string output, string language)
    {
        if (nodeTypes.Count == 0) return 1.0;

        var covered = 0;
        foreach (var type in nodeTypes)
        {
            if (output.Contains(type, StringComparison.OrdinalIgnoreCase))
            {
                covered++;
            }
        }

        return (double)covered / nodeTypes.Count;
    }

    private static double CheckExpressionCoverage(HashSet<string> expressionTypes, string output)
    {
        if (expressionTypes.Count == 0) return 1.0;

        var covered = 0;
        foreach (var type in expressionTypes)
        {
            var keyword = type.Replace("Node", "");
            if (output.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                covered++;
            }
        }

        return (double)covered / expressionTypes.Count;
    }

    private static double CheckFilterCoverage(HashSet<string> filters, string output)
    {
        if (filters.Count == 0) return 1.0;

        var covered = 0;
        foreach (var filter in filters)
        {
            if (output.Contains($"\"{filter}\"") || output.Contains($"\"{filter}\""))
            {
                covered++;
            }
        }

        return (double)covered / filters.Count;
    }
}
