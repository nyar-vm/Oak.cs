using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     系统声明语法节点：system Name { ... }
/// </summary>
public sealed class SystemSyntax : ValkyrieDeclarationSyntax
{
    public SystemSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>system 关键字</summary>
    public SyntaxToken SystemKeyword => ChildToken(0);

    /// <summary>系统名称</summary>
    public SyntaxToken Name => ChildToken(1);

    /// <summary>左大括号</summary>
    public SyntaxToken OpenBrace => ChildToken(2);

    /// <summary>右大括号</summary>
    public SyntaxToken CloseBrace => LastToken();
}