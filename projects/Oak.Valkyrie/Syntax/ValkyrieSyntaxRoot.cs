using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     Valkyrie 语法树根节点，对应 CompilationUnit
/// </summary>
public sealed class ValkyrieSyntaxRoot : SyntaxRoot
{
    private readonly int _childCount;
    private readonly SyntaxTree _syntaxTree;

    /// <summary>
    ///     初始化 Valkyrie 语法根节点
    /// </summary>
    public ValkyrieSyntaxRoot(GreenNode green, SyntaxTree tree, int offset)
        : base(green, tree, offset, "Valkyrie")
    {
        _childCount = green.ChildCount;
        _syntaxTree = tree;
    }

    /// <summary>
    ///     子节点数量
    /// </summary>
    public int ChildCount => _childCount;

    /// <summary>
    ///     所有顶层声明节点
    /// </summary>
    public IReadOnlyList<ValkyrieDeclarationSyntax> Declarations
    {
        get
        {
            var decls = new List<ValkyrieDeclarationSyntax>();
            for (var i = 0; i < _childCount; i++)
            {
                try
                {
                    var child = ChildNode<ValkyrieDeclarationSyntax>(i);
                    decls.Add(child);
                }
                catch
                {
                    // 子节点不是声明类型，跳过
                }
            }

            return decls;
        }
    }

    /// <summary>
    ///     获取指定索引的子节点
    /// </summary>
    public SyntaxNode GetChild(int index)
    {
        return ChildNode<SyntaxNode>(index);
    }

    public override VisitRecursionMode Accept(SyntaxVisitor visitor)
    {
        return visitor.VisitDefault(this);
    }
}
