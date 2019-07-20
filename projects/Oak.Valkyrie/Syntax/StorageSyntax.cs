using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     存储声明语法节点：storage Name { ... }
/// </summary>
public sealed class StorageSyntax : ValkyrieDeclarationSyntax
{
    public StorageSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>storage 关键字</summary>
    public SyntaxToken StorageKeyword => ChildToken(0);

    /// <summary>存储名称</summary>
    public SyntaxToken Name => ChildToken(1);

    /// <summary>左大括号</summary>
    public SyntaxToken OpenBrace => ChildToken(2);

    /// <summary>右大括号</summary>
    public SyntaxToken CloseBrace => LastToken();
}