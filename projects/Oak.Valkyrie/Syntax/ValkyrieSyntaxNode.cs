using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     Valkyrie 语法节点抽象基类，扩展 Oak SyntaxNode 提供 Valkyrie 特定的导航能力
/// </summary>
public abstract class ValkyrieSyntaxNode : SyntaxNode
{
    /// <summary>
    ///     子节点数量（从构造函数参数 GreenNode 捕获，因 SyntaxNode.Green 为 internal 不可访问）
    /// </summary>
    private readonly int _childCount;

    /// <summary>
    ///     初始化 Valkyrie 语法节点
    /// </summary>
    protected ValkyrieSyntaxNode(GreenNode green, SyntaxTree tree, int offset)
        : base(green, tree, offset)
    {
        _childCount = green.ChildCount;
    }

    /// <summary>
    ///     子节点数量
    /// </summary>
    public int ChildCount => _childCount;

    /// <summary>
    ///     收集所有指定类型的子节点
    /// </summary>
    protected IReadOnlyList<T> CollectChildren<T>() where T : SyntaxNode
    {
        var result = new List<T>();
        for (var i = 0; i < _childCount; i++)
        {
            try
            {
                var child = ChildNode<T>(i);
                if (child is T t)
                {
                    result.Add(t);
                }
            }
            catch
            {
                // 子节点类型不匹配，跳过
            }
        }

        return result;
    }

    /// <summary>
    ///     获取最后一枚子标记
    /// </summary>
    protected SyntaxToken LastToken()
    {
        return ChildToken(_childCount - 1);
    }

    public override VisitRecursionMode Accept(SyntaxVisitor visitor)
    {
        return visitor.VisitDefault(this);
    }
}
