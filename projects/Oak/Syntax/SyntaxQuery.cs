using System.Collections;

namespace Oak.Syntax;

/// <summary>
///     语法树惰性查询器，支持链式查询与类型过滤
/// </summary>
public sealed class SyntaxQuery : IEnumerable<SyntaxNode>
{
    private readonly SyntaxNode _root;
    private readonly Func<SyntaxNode, bool>? _predicate;

    private SyntaxQuery(SyntaxNode root, Func<SyntaxNode, bool>? predicate)
    {
        _root = root;
        _predicate = predicate;
    }

    /// <summary>
    ///     从指定节点创建查询
    /// </summary>
    public static SyntaxQuery From(SyntaxNode root)
    {
        return new SyntaxQuery(root, null);
    }

    /// <summary>
    ///     按条件过滤
    /// </summary>
    public SyntaxQuery Where(Func<SyntaxNode, bool> predicate)
    {
        var combined = _predicate is not null
            ? node => _predicate(node) && predicate(node)
            : predicate;
        return new SyntaxQuery(_root, combined);
    }

    /// <summary>
    ///     按类型过滤并转为强类型枚举
    /// </summary>
    public IEnumerable<T> OfType<T>() where T : SyntaxNode
    {
        foreach (var node in this)
        {
            if (node is T typed)
            {
                yield return typed;
            }
        }
    }

    /// <summary>
    ///     投影转换
    /// </summary>
    public IEnumerable<TResult> Select<TResult>(Func<SyntaxNode, TResult> selector)
    {
        foreach (var node in this)
        {
            yield return selector(node);
        }
    }

    /// <summary>
    ///     获取所有后代节点
    /// </summary>
    public IEnumerable<T> DescendantsOfType<T>() where T : SyntaxNode
    {
        foreach (var node in EnumerateDescendants(_root))
        {
            if (node is T typed)
            {
                yield return typed;
            }
        }
    }

    /// <summary>
    ///     获取所有后代节点（含自身）
    /// </summary>
    public IEnumerable<SyntaxNode> Descendants()
    {
        return EnumerateDescendants(_root);
    }

    /// <inheritdoc />
    public IEnumerator<SyntaxNode> GetEnumerator()
    {
        return EnumerateDescendants(_root).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IEnumerable<SyntaxNode> EnumerateDescendants(SyntaxNode node)
    {
        if (Filter(node))
        {
            yield return node;
        }

        var greenNode = node.Green;
        var childCount = greenNode.ChildCount;
        var offset = node.Span.Start;

        for (var i = 0; i < childCount; i++)
        {
            var child = greenNode.GetChild(i);
            if (child is null)
            {
                continue;
            }

            var childNode = NodeFactory.Create(child.Kind, child, node.Tree, offset + ChildOffset(greenNode, i));
            if (childNode is null)
            {
                continue;
            }

            foreach (var descendant in EnumerateDescendants(childNode))
            {
                yield return descendant;
            }
        }
    }

    private bool Filter(SyntaxNode node)
    {
        return _predicate is null || _predicate(node);
    }

    /// <summary>
    ///     计算第 index 个子节点之前的累计 Width（偏移量）
    /// </summary>
    private static int ChildOffset(GreenNode parent, int targetIndex)
    {
        var offset = 0;
        for (var i = 0; i < targetIndex; i++)
        {
            var child = parent.GetChild(i);
            if (child is not null)
            {
                offset += child.Width;
            }
        }

        return offset;
    }
}
