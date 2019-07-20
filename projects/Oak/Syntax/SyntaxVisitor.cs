namespace Oak.Syntax;

/// <summary>
///     语法树访问递归模式
/// </summary>
public enum VisitRecursionMode
{
    /// <summary>
    ///     继续遍历子节点
    /// </summary>
    Continue,

    /// <summary>
    ///     跳过当前节点的子节点，继续遍历兄弟节点
    /// </summary>
    Skip,

    /// <summary>
    ///     停止遍历，不再访问任何后续节点
    /// </summary>
    Stop
}

/// <summary>
///     语法树访问器抽象基类，支持短路遍历控制
/// </summary>
public abstract class SyntaxVisitor
{
    /// <summary>
    ///     访问语法节点的默认行为，返回递归模式
    /// </summary>
    public virtual VisitRecursionMode VisitDefault(SyntaxNode node)
    {
        return VisitRecursionMode.Continue;
    }

    /// <summary>
    ///     从指定节点开始深度优先遍历，返回最终的递归模式
    /// </summary>
    public VisitRecursionMode Visit(SyntaxNode node)
    {
        var mode = node.Accept(this);

        if (mode == VisitRecursionMode.Stop)
        {
            return VisitRecursionMode.Stop;
        }

        if (mode == VisitRecursionMode.Skip)
        {
            return VisitRecursionMode.Continue;
        }

        return VisitChildren(node);
    }

    /// <summary>
    ///     遍历节点的所有子节点，根据访问结果控制递归行为
    /// </summary>
    protected VisitRecursionMode VisitChildren(SyntaxNode node)
    {
        var greenNode = node.Green;
        var childCount = greenNode.ChildCount;
        var offset = node.Span.Start;

        var childOffset = offset;

        for (var i = 0; i < childCount; i++)
        {
            var childGreen = greenNode.GetChild(i);

            if (childGreen is null)
            {
                continue;
            }

            var childNode = NodeFactory.Create(childGreen.Kind, childGreen, node.Tree, childOffset);

            if (childNode is null)
            {
                childOffset += childGreen.Width;
                continue;
            }

            var result = Visit(childNode);

            if (result == VisitRecursionMode.Stop)
            {
                return VisitRecursionMode.Stop;
            }

            childOffset += childGreen.Width;
        }

        return VisitRecursionMode.Continue;
    }
}
