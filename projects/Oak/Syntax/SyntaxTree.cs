using Oak.Syntax;
namespace Oak.Syntax;

/// <summary>
///     语法树，支持增量编辑和可选的父节点缓存
/// </summary>
public class SyntaxTree
{
    private readonly List<SyntaxRoot> _allRoots = [];

    private readonly Dictionary<(GreenNode, int), RedNode>? _parentCache;

    /// <summary>
    ///     构造语法树
    /// </summary>
    public SyntaxTree(ISource source, GreenNode root, bool enableParentCache = false)
    {
        Source = source;
        Root = root;
        EnableParentCache = enableParentCache;

        if (enableParentCache)
        {
            _parentCache = new Dictionary<(GreenNode, int), RedNode>();
            BuildParentCache(new RedNode(root, this, 0), null);
        }
    }

    /// <summary>
    ///     源文本
    /// </summary>
    public ISource Source { get; }

    /// <summary>
    ///     绿树根节点
    /// </summary>
    public GreenNode Root { get; }

    /// <summary>
    ///     是否启用父节点缓存
    /// </summary>
    public bool EnableParentCache { get; }

    /// <summary>
    ///     所有语法根
    /// </summary>
    public IReadOnlyList<SyntaxRoot> AllRoots => _allRoots;

    /// <summary>
    ///     主语法根
    /// </summary>
    public SyntaxRoot? PrimaryRoot => _allRoots.Count > 0 ? _allRoots[0] : null;

    /// <summary>
    ///     构建父节点缓存
    /// </summary>
    private void BuildParentCache(RedNode node, RedNode? parent)
    {
        if (parent is not null) _parentCache![(node.Green, node.Start)] = parent.Value;

        for (var i = 0; i < node.ChildCount; i++) BuildParentCache(node.GetChild(i), node);
    }

    /// <summary>
    ///     尝试从缓存中获取父节点
    /// </summary>
    internal bool TryGetParent(RedNode node, out RedNode? parent)
    {
        if (_parentCache is not null)
        {
            if (_parentCache.TryGetValue((node.Green, node.Start), out var p))
            {
                parent = p;
                return true;
            }

            parent = null;
            return false;
        }

        parent = null;
        return false;
    }

    /// <summary>
    ///     获取红树根节点
    /// </summary>
    public RedNode GetRedRoot()
    {
        return new RedNode(Root, this, 0);
    }

    /// <summary>
    ///     应用编辑并尝试增量重解析
    /// </summary>
    public SyntaxTree Edit(Edit edit, IncrementalParserRepo parsers)
    {
        var affectedNode = FindDeepestNode(Root, edit.OldSpan, 0);
        if (affectedNode is null) return this;

        var oldRoot = Root;
        var newRoot = TryIncrementalReparse(Root, affectedNode, edit, parsers, 0);
        var newSource = ApplyEdit(edit);
        var newTree = new SyntaxTree(newSource, newRoot, EnableParentCache);

        if (!ReferenceEquals(oldRoot, newRoot))
        {
            var replaced = CollectReplacedNodes(oldRoot, newRoot);
            var changeEvent = new TreeChangeEvent(this, newTree, edit.OldSpan, replaced, edit);
            newTree.OnChanged(changeEvent);
        }

        return newTree;
    }

    /// <summary>
    ///     语法树变更事件
    /// </summary>
    public event Action<TreeChangeEvent>? Changed;

    /// <summary>
    ///     触发变更事件
    /// </summary>
    private void OnChanged(TreeChangeEvent e)
    {
        Changed?.Invoke(e);
    }

    /// <summary>
    ///     收集被替换的绿树节点
    /// </summary>
    private static List<GreenNode> CollectReplacedNodes(GreenNode oldRoot, GreenNode newRoot)
    {
        var replaced = new List<GreenNode>();
        CollectDiff(oldRoot, newRoot, replaced);
        return replaced;
    }

    /// <summary>
    ///     递归比较两棵绿树，收集差异节点
    /// </summary>
    private static void CollectDiff(GreenNode oldNode, GreenNode newNode, List<GreenNode> replaced)
    {
        if (!ReferenceEquals(oldNode, newNode)) replaced.Add(oldNode);

        var minCount = Math.Min(oldNode.ChildCount, newNode.ChildCount);
        for (var i = 0; i < minCount; i++)
        {
            var oldChild = oldNode.GetChild(i);
            var newChild = newNode.GetChild(i);
            if (oldChild is not null && newChild is not null && !ReferenceEquals(oldChild, newChild))
                CollectDiff(oldChild, newChild, replaced);
        }
    }

    /// <summary>
    ///     根据语言标识获取语法根
    /// </summary>
    public SyntaxRoot? GetRoot(string languageId)
    {
        foreach (var root in _allRoots)
            if (root.LanguageId == languageId)
                return root;

        return null;
    }

    /// <summary>
    ///     添加语法根
    /// </summary>
    internal void AddRoot(SyntaxRoot root)
    {
        _allRoots.Add(root);
    }

    /// <summary>
    ///     查找与指定跨度重叠的最深节点
    /// </summary>
    private GreenNode? FindDeepestNode(GreenNode node, TextSpan span, int offset)
    {
        var nodeSpan = new TextSpan(offset, node.Width);
        if (!nodeSpan.OverlapsWith(span) && !nodeSpan.Contains(span.Start)) return null;

        var childOffset = offset;
        for (var i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is not null)
            {
                var deeper = FindDeepestNode(child, span, childOffset);
                if (deeper is not null) return deeper;
                childOffset += child.Width;
            }
        }

        return node;
    }

    /// <summary>
    ///     尝试对受影响节点进行增量重解析，失败则向上冒泡至父节点
    /// </summary>
    private GreenNode TryIncrementalReparse(GreenNode root, GreenNode affected, Edit edit,
        IncrementalParserRepo parsers, int offset)
    {
        var parser = parsers.Get(affected.Kind);
        if (parser is not null)
        {
            var newSource = ApplyEdit(edit);
            var newSpan = new TextSpan(edit.OldSpan.Start, edit.OldSpan.Length);
            var result = parser(newSource, newSpan, null, out var changed);
            if (result is not null && changed) return ReplaceNode(root, affected, result);
        }

        var parent = FindParentGreen(root, affected);
        if (parent is not null)
        {
            var parentOffset = FindNodeOffset(root, parent, 0);
            return TryIncrementalReparse(root, parent, edit, parsers, parentOffset);
        }

        return root;
    }

    /// <summary>
    ///     在绿树中将目标节点替换为新节点，复用未变更的子节点
    /// </summary>
    private static GreenNode ReplaceNode(GreenNode root, GreenNode target, GreenNode replacement)
    {
        if (ReferenceEquals(root, target))
        {
            return replacement;
        }

        if (root.ChildCount == 0)
        {
            return root;
        }

        var anyChanged = false;
        var children = new GreenNode[root.ChildCount];

        for (var i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChild(i);

            if (child is not null)
            {
                var newChild = ReplaceNode(child, target, replacement);
                children[i] = newChild;

                if (!ReferenceEquals(child, newChild))
                {
                    anyChanged = true;
                }
            }
        }

        if (!anyChanged)
        {
            return root;
        }

        return new GreenInternalNode(root.Kind, children);
    }

    /// <summary>
    ///     在绿树中查找目标节点的父节点
    /// </summary>
    private static GreenNode? FindParentGreen(GreenNode root, GreenNode target)
    {
        for (var i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChild(i);
            if (ReferenceEquals(child, target)) return root;
            if (child is not null)
            {
                var found = FindParentGreen(child, target);
                if (found is not null) return found;
            }
        }

        return null;
    }

    /// <summary>
    ///     在绿树中查找目标节点的偏移量
    /// </summary>
    private static int FindNodeOffset(GreenNode root, GreenNode target, int offset)
    {
        if (ReferenceEquals(root, target)) return offset;
        var childOffset = offset;
        for (var i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChild(i);
            if (child is not null)
            {
                var found = FindNodeOffset(child, target, childOffset);
                if (found >= 0) return found;
                childOffset += child.Width;
            }
        }

        return -1;
    }

    /// <summary>
    ///     应用编辑到源文本，生成新的 StringSource
    /// </summary>
    private StringSource ApplyEdit(Edit edit)
    {
        var original = Source.Substring(new Range(0, Source.Length));
        var before = original[..edit.OldSpan.Start];
        var after = original[edit.OldSpan.End..];
        return new StringSource(before + edit.NewText + after);
    }
}