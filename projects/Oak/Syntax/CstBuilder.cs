namespace Oak.Syntax;

/// <summary>
///     Concrete Syntax Tree 构建器。
///     栈分配（ref struct），用于高效构建 Green 树。
///     用法：BeginNode → AddLeaf/AddNode → EndNode 循环 → Build
/// </summary>
public ref struct CstBuilder
{
    private GreenNode[] _currentChildren;
    private int _childCount;
    private readonly StackEntry[] _stack;
    private int _stackDepth;

    /// <summary>
    ///     使用指定的最大子节点容量和最大嵌套深度创建构建器
    /// </summary>
    /// <param name="maxChildCapacity">最大子节点容量</param>
    /// <param name="maxNestingDepth">最大嵌套深度</param>
    public CstBuilder(int maxChildCapacity = 256, int maxNestingDepth = 64)
    {
        _currentChildren = new GreenNode[maxChildCapacity];
        _childCount = 0;
        _stack = new StackEntry[maxNestingDepth];
        _stackDepth = 0;
    }

    /// <summary>
    ///     开始构建一个内部节点
    /// </summary>
    /// <param name="kind">节点类型</param>
    public void BeginNode(NodeKind kind)
    {
        _stack[_stackDepth] = new StackEntry
        {
            Kind = kind,
            SavedChildren = _currentChildren,
            SavedChildCount = _childCount
        };

        _stackDepth++;
        _currentChildren = new GreenNode[_currentChildren.Length];
        _childCount = 0;
    }

    /// <summary>
    ///     BeginNode 的别名，兼容旧 API
    /// </summary>
    /// <param name="kind">节点类型</param>
    public void StartNode(NodeKind kind) => BeginNode(kind);

    /// <summary>
    ///     添加一个叶子节点
    /// </summary>
    /// <param name="kind">叶子节点类型</param>
    /// <param name="width">源码中的字符宽度</param>
    /// <param name="text">叶子节点文本内容</param>
    public void AddLeaf(NodeKind kind, int width, string? text = null)
    {
        if (_childCount >= _currentChildren.Length)
        {
            throw new InvalidOperationException("子节点数量超过容量限制");
        }

        _currentChildren[_childCount] = new GreenLeafNode(kind, width, text);
        _childCount++;
    }

    /// <summary>
    ///     AddLeaf 的别名（width = text?.Length ?? 0），兼容旧 API
    /// </summary>
    /// <param name="kind">叶子节点类型</param>
    /// <param name="text">叶子节点文本内容</param>
    public void AddToken(NodeKind kind, string? text = null) => AddLeaf(kind, text?.Length ?? 0, text);

    /// <summary>
    ///     使用 TextSpan 的 Length 作为 width 的 AddLeaf 别名
    /// </summary>
    /// <param name="kind">叶子节点类型</param>
    /// <param name="span">文本跨度（Length 作为 width，Offset 忽略）</param>
    public void AddToken(NodeKind kind, TextSpan span) => AddLeaf(kind, span.Length);

    /// <summary>
    ///     添加一个已有的 Green 节点（叶子或内部节点）
    /// </summary>
    /// <param name="node">Green 节点</param>
    public void AddNode(GreenNode node)
    {
        if (_childCount >= _currentChildren.Length)
        {
            throw new InvalidOperationException("子节点数量超过容量限制");
        }

        _currentChildren[_childCount] = node;
        _childCount++;
    }

    /// <summary>
    ///     AddNode 的别名，兼容旧 API
    /// </summary>
    /// <param name="node">Green 节点</param>
    public void AddChild(GreenNode node) => AddNode(node);

    /// <summary>
    ///     结束当前节点的构建，将其作为子节点加入父节点
    /// </summary>
    public void EndNode()
    {
        if (_stackDepth <= 0)
        {
            throw new InvalidOperationException("没有正在构建的节点");
        }

        _stackDepth--;

        var currentChildren = new GreenNode[_childCount];
        for (var i = 0; i < _childCount; i++)
        {
            currentChildren[i] = _currentChildren[i];
        }

        var node = new GreenInternalNode(
            _stack[_stackDepth].Kind,
            currentChildren
        );

        _currentChildren = _stack[_stackDepth].SavedChildren;
        _childCount = _stack[_stackDepth].SavedChildCount;

        AddNode(node);
    }

    /// <summary>
    ///     构建并返回根节点。
    ///     如果只有一个根级子节点，直接返回它；
    ///     否则包装为一个 Module 内部节点
    /// </summary>
    /// <returns>Green 树根节点</returns>
    public GreenNode Build()
    {
        if (_stackDepth != 0)
        {
            throw new InvalidOperationException($"还有 {_stackDepth} 个未结束的节点");
        }

        if (_childCount == 1)
        {
            return _currentChildren[0];
        }

        var children = new GreenNode[_childCount];
        for (var i = 0; i < _childCount; i++)
        {
            children[i] = _currentChildren[i];
        }

        return new GreenInternalNode((NodeKind)0, children);
    }

    private struct StackEntry
    {
        public NodeKind Kind;
        public GreenNode[] SavedChildren;
        public int SavedChildCount;
    }
}
