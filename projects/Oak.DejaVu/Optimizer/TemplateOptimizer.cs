using Oak.DejaVu.Expressions;

namespace Oak.DejaVu.Optimizer;

/// <summary>
///     模板优化器——死代码消除、节点合并
/// </summary>
public sealed class TemplateOptimizer
{
    /// <summary>
    ///     优化模板 AST
    /// </summary>
    public List<DejaVuTemplateNode> Optimize(List<DejaVuTemplateNode> nodes)
    {
        var optimized = new List<DejaVuTemplateNode>();

        foreach (var node in nodes)
        {
            var optimizedNode = OptimizeNode(node);
            if (optimizedNode != null)
            {
                if (optimizedNode is IEnumerable<DejaVuTemplateNode> flattened)
                {
                    optimized.AddRange(flattened);
                }
                else
                {
                    optimized.Add(optimizedNode);
                }
            }
        }

        return MergeConsecutiveTextNodes(optimized);
    }

    /// <summary>
    ///     优化单个节点
    /// </summary>
    private DejaVuTemplateNode? OptimizeNode(DejaVuTemplateNode node)
    {
        return node switch
        {
            DejaVuIfNode ifNode => OptimizeIfNode(ifNode),
            DejaVuLoopNode loopNode => OptimizeLoopNode(loopNode),
            DejaVuBlockNode blockNode => OptimizeBlockNode(blockNode),
            DejaVuLetNode letNode => OptimizeLetNode(letNode),
            DejaVuWithNode withNode => OptimizeWithNode(withNode),
            DejaVuRawNode rawNode => OptimizeRawNode(rawNode),
            DejaVuCodeNode codeNode => OptimizeCodeNode(codeNode),
            _ => node
        };
    }

    /// <summary>
    ///     优化 if 节点——永假分支消除
    /// </summary>
    private DejaVuTemplateNode? OptimizeIfNode(DejaVuIfNode ifNode)
    {
        var optimizedCondition = OptimizeExpression(ifNode.ParsedCondition);

        // 常量折叠后的条件检查
        if (IsConstantFalse(optimizedCondition))
        {
            // 条件恒为 false，跳过 if 体，检查 else if / else
            foreach (var elseIfNode in ifNode.ElseIfNodes)
            {
                var optimizedElseIf = OptimizeExpression(elseIfNode.ParsedCondition);
                if (!IsConstantFalse(optimizedElseIf))
                {
                    return new DejaVuCodeNode
                    {
                        Code = elseIfNode.Condition,
                        ParsedExpression = OptimizeExpression(optimizedElseIf)
                    };
                }
            }

            if (ifNode.ElseChildren.Count > 0)
            {
                var optimizedElse = Optimize(ifNode.ElseChildren);
                return optimizedElse.Count == 1 ? optimizedElse[0] : null;
            }

            return null; // 整棵 if 树删除
        }

        var children = Optimize(ifNode.Children);
        var elseChildren = Optimize(ifNode.ElseChildren);
        var elseIfNodes = new List<DejaVuElseIfNode>();

        foreach (var elseIfNode in ifNode.ElseIfNodes)
        {
            var optimizedElseIf = OptimizeExpression(elseIfNode.ParsedCondition);
            if (!IsConstantFalse(optimizedElseIf))
            {
                elseIfNodes.Add(new DejaVuElseIfNode
                {
                    Condition = elseIfNode.Condition,
                    ParsedCondition = optimizedElseIf,
                    Children = Optimize(elseIfNode.Children)
                });
            }
        }

        return new DejaVuIfNode
        {
            Condition = ifNode.Condition,
            ParsedCondition = OptimizeExpression(optimizedCondition),
            Children = children,
            ElseChildren = elseChildren,
            ElseIfNodes = elseIfNodes
        };
    }

    /// <summary>
    ///     优化 loop 节点
    /// </summary>
    private DejaVuTemplateNode OptimizeLoopNode(DejaVuLoopNode loopNode)
    {
        return new DejaVuLoopNode
        {
            Expression = loopNode.Expression,
            ParsedExpression = OptimizeExpression(loopNode.ParsedExpression),
            ItemName = loopNode.ItemName,
            Children = Optimize(loopNode.Children)
        };
    }

    /// <summary>
    ///     优化 block 节点
    /// </summary>
    private DejaVuTemplateNode OptimizeBlockNode(DejaVuBlockNode blockNode)
    {
        return new DejaVuBlockNode
        {
            Name = blockNode.Name,
            Children = Optimize(blockNode.Children)
        };
    }

    /// <summary>
    ///     优化 let 节点
    /// </summary>
    private DejaVuTemplateNode OptimizeLetNode(DejaVuLetNode letNode)
    {
        return new DejaVuLetNode
        {
            VariableName = letNode.VariableName,
            Expression = letNode.Expression,
            ParsedExpression = OptimizeExpression(letNode.ParsedExpression),
            Children = Optimize(letNode.Children)
        };
    }

    /// <summary>
    ///     优化 with 节点
    /// </summary>
    private DejaVuTemplateNode OptimizeWithNode(DejaVuWithNode withNode)
    {
        return new DejaVuWithNode
        {
            AliasName = withNode.AliasName,
            Expression = withNode.Expression,
            ParsedExpression = OptimizeExpression(withNode.ParsedExpression),
            Children = Optimize(withNode.Children)
        };
    }

    /// <summary>
    ///     优化 raw 节点
    /// </summary>
    private DejaVuTemplateNode OptimizeRawNode(DejaVuRawNode rawNode)
    {
        return new DejaVuRawNode { Children = Optimize(rawNode.Children) };
    }

    /// <summary>
    ///     优化 code 节点
    /// </summary>
    private DejaVuTemplateNode OptimizeCodeNode(DejaVuCodeNode codeNode)
    {
        var optimized = OptimizeExpression(codeNode.ParsedExpression);
        if (optimized is LiteralNode lit)
        {
            return new DejaVuTextNode { Text = lit.Value?.ToString() ?? string.Empty };
        }

        return new DejaVuCodeNode { Code = codeNode.Code, ParsedExpression = optimized };
    }

    /// <summary>
    ///     判断表达式是否为常量 false
    /// </summary>
    private static bool IsConstantFalse(IExpressionNode? node)
    {
        return node is LiteralNode { Value: false };
    }

    /// <summary>
    ///     判断表达式是否为常量 true
    /// </summary>
    private static bool IsConstantTrue(IExpressionNode? node)
    {
        return node is LiteralNode { Value: true };
    }

    /// <summary>
    ///     合并连续文本节点
    /// </summary>
    private static List<DejaVuTemplateNode> MergeConsecutiveTextNodes(List<DejaVuTemplateNode> nodes)
    {
        if (nodes.Count < 2)
        {
            return nodes;
        }

        var result = new List<DejaVuTemplateNode>();
        var sb = new System.Text.StringBuilder();

        foreach (var node in nodes)
        {
            if (node is DejaVuTextNode textNode)
            {
                sb.Append(textNode.Text);
            }
            else
            {
                if (sb.Length > 0)
                {
                    result.Add(new DejaVuTextNode { Text = sb.ToString() });
                    sb.Clear();
                }

                result.Add(node);
            }
        }

        if (sb.Length > 0)
        {
            result.Add(new DejaVuTextNode { Text = sb.ToString() });
        }

        return result;
    }

    /// <summary>
    ///     优化表达式（常量折叠包装）
    /// </summary>
    private static IExpressionNode? OptimizeExpression(IExpressionNode? node)
    {
        return node == null ? null : ExpressionOptimizer.Optimize(node);
    }
}
