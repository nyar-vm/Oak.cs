using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     命名空间声明语法节点：namespace Name { ... }
/// </summary>
public sealed class NamespaceSyntax : ValkyrieDeclarationSyntax
{
    public NamespaceSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>namespace 关键字</summary>
    public SyntaxToken NamespaceKeyword => ChildToken(0);

    /// <summary>命名空间名称</summary>
    public SyntaxToken Name => ChildToken(1);

    /// <summary>左大括号</summary>
    public SyntaxToken OpenBrace => ChildToken(2);

    /// <summary>右大括号</summary>
    public SyntaxToken CloseBrace => LastToken();
}