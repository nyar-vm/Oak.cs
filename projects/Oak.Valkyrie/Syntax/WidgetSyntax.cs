using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     控件声明语法节点：widget Name { ... }
/// </summary>
public sealed class WidgetSyntax : ValkyrieDeclarationSyntax
{
    public WidgetSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>widget 关键字</summary>
    public SyntaxToken WidgetKeyword => ChildToken(0);

    /// <summary>控件名称</summary>
    public SyntaxToken Name => ChildToken(1);

    /// <summary>左大括号</summary>
    public SyntaxToken OpenBrace => ChildToken(2);

    /// <summary>右大括号</summary>
    public SyntaxToken CloseBrace => LastToken();
}