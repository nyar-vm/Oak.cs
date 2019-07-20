using Oak.Syntax;
namespace Oak.Syntax;

/// <summary>
///     强类型 AST 节点的抽象基类
/// </summary>
public abstract class SyntaxNode
{
    /// <summary>
    ///     初始化语法节点
    /// </summary>
    protected SyntaxNode(GreenNode green, SyntaxTree tree, int offset)
    {
        Green = green;
        Tree = tree;
        Offset = offset;
    }

    /// <summary>
    ///     对应的绿树节点
    /// </summary>
    internal GreenNode Green { get; }

    /// <summary>
    ///     所属语法树
    /// </summary>
    internal SyntaxTree Tree { get; private set; }

    /// <summary>
    ///     相对于语法树根节点的偏移量
    /// </summary>
    internal int Offset { get; }

    /// <summary>
    ///     节点在源文本中的范围
    /// </summary>
    public TextSpan Span => new(Offset, Green.Width);

    /// <summary>
    ///     将此节点绑定到指定的语法树
    /// </summary>
    protected void BindToTree(SyntaxTree tree, int offset)
    {
        Tree = tree;
    }

    /// <summary>
    ///     获取指定索引的子节点，并包装为强类型。
    ///     优先使用 NodeFactory 查表构造，若未注册则回退到反射。
    /// </summary>
    protected T ChildNode<T>(int index) where T : SyntaxNode
    {
        var childGreen = Green.GetChild(index);
        var childOffset = ComputeChildOffset(index);

        var factory = NodeFactory.Get(childGreen!.Kind);
        if (factory is not null) return (T)factory(childGreen, Tree, childOffset);

        return (T)Activator.CreateInstance(typeof(T), childGreen, Tree, childOffset)!;
    }

    /// <summary>
    ///     获取指定索引的子标记
    /// </summary>
    protected SyntaxToken ChildToken(int index)
    {
        var childGreen = Green.GetChild(index);
        var childOffset = ComputeChildOffset(index);
        var text = childGreen is GreenLeafNode leaf ? leaf.Text ?? string.Empty : string.Empty;
        return new SyntaxToken(childGreen!.Kind, default, text);
    }

    /// <summary>
    ///     计算指定索引子节点的偏移量
    /// </summary>
    private int ComputeChildOffset(int index)
    {
        var childOffset = Offset;
        for (var i = 0; i < index; i++)
        {
            var sibling = Green.GetChild(i);
            if (sibling is not null) childOffset += sibling.Width;
        }

        return childOffset;
    }

    /// <summary>
    ///     接受访问器访问，返回递归模式以控制遍历行为
    /// </summary>
    public abstract VisitRecursionMode Accept(SyntaxVisitor visitor);

    /// <summary>
    ///     接受访问器访问的默认实现，返回继续遍历
    /// </summary>
    public virtual VisitRecursionMode AcceptVisitor(SyntaxVisitor visitor)
    {
        return Accept(visitor);
    }
}