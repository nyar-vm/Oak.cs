using Oak.Syntax;
namespace Oak.Syntax;

/// <summary>
///     轻量级值类型，持有绝对位置信息的红树节点
/// </summary>
public readonly struct RedNode
{
    /// <summary>
    ///     对应的绿树节点
    /// </summary>
    internal GreenNode Green { get; }

    /// <summary>
    ///     所属的语法树
    /// </summary>
    internal SyntaxTree Tree { get; }

    /// <summary>
    ///     节点在源文本中的起始位置
    /// </summary>
    internal int Start { get; }

    /// <summary>
    ///     节点类型
    /// </summary>
    public NodeKind Kind => Green.Kind;

    /// <summary>
    ///     节点在源文本中的跨度
    /// </summary>
    public TextSpan Span => new(Start, Green.Width);

    /// <summary>
    ///     子节点数量
    /// </summary>
    public int ChildCount => Green.ChildCount;

    /// <summary>
    ///     是否为叶子节点
    /// </summary>
    public bool IsLeaf => Green.IsLeaf;

    /// <summary>
    ///     构造红树节点
    /// </summary>
    internal RedNode(GreenNode green, SyntaxTree tree, int start)
    {
        Green = green;
        Tree = tree;
        Start = start;
    }

    /// <summary>
    ///     父节点。若语法树启用了父节点缓存，则查表获取；否则遍历查找。
    /// </summary>
    public RedNode? Parent
    {
        get
        {
            if (Tree is null) return null;

            if (Tree.TryGetParent(this, out var parent)) return parent;

            var root = Tree.GetRedRoot();
            return FindParent(root, this);
        }
    }

    /// <summary>
    ///     获取指定索引的子节点
    /// </summary>
    public RedNode GetChild(int index)
    {
        var childGreen = Green.GetChild(index);
        var childStart = Start;
        for (var i = 0; i < index; i++)
        {
            var sibling = Green.GetChild(i);
            if (sibling is not null) childStart += sibling.Width;
        }

        return new RedNode(childGreen!, Tree, childStart);
    }

    /// <summary>
    ///     所有子节点
    /// </summary>
    public IEnumerable<RedNode> Children
    {
        get
        {
            for (var i = 0; i < ChildCount; i++) yield return GetChild(i);
        }
    }

    /// <summary>
    ///     所有后代节点（深度优先遍历）
    /// </summary>
    public IEnumerable<RedNode> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.Descendants()) yield return descendant;
        }
    }

    /// <summary>
    ///     所有祖先节点
    /// </summary>
    public IEnumerable<RedNode> Ancestors()
    {
        var current = Parent;
        while (current is not null)
        {
            yield return current.Value;
            current = current.Value.Parent;
        }
    }

    /// <summary>
    ///     在以 root 为根的子树中查找 target 的父节点
    /// </summary>
    private static RedNode? FindParent(RedNode root, RedNode target)
    {
        for (var i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChild(i);
            if (ReferenceEquals(child.Green, target.Green) && child.Start == target.Start) return root;
            var found = FindParent(child, target);
            if (found is not null) return found;
        }

        return null;
    }
}