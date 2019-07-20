using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     组件声明语法节点：component Name { ...fields... }
/// </summary>
public sealed class ComponentSyntax : ValkyrieDeclarationSyntax
{
    public ComponentSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>component 关键字</summary>
    public SyntaxToken ComponentKeyword => ChildToken(0);

    /// <summary>组件名称</summary>
    public SyntaxToken Name => ChildToken(1);

    /// <summary>左大括号</summary>
    public SyntaxToken OpenBrace => ChildToken(2);

    /// <summary>字段列表（位于索引 3 至 ChildCount-2）</summary>
    public IReadOnlyList<FieldSyntax> Fields
    {
        get
        {
            var fields = new List<FieldSyntax>();
            // 子节点布局：[0]Keyword [1]Name [2]LeftBrace [3..n-2]Fields [n-1]RightBrace
            for (var i = 3; i < ChildCount - 1; i++)
            {
                fields.Add(ChildNode<FieldSyntax>(i));
            }

            return fields;
        }
    }

    /// <summary>右大括号</summary>
    public SyntaxToken CloseBrace => LastToken();
}