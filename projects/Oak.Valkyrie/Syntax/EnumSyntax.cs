using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     枚举声明语法节点：enum Name { ... }
/// </summary>
public sealed class EnumSyntax : ValkyrieDeclarationSyntax
{
    public EnumSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>enum 关键字</summary>
    public SyntaxToken EnumKeyword => ChildToken(0);

    /// <summary>枚举名称</summary>
    public SyntaxToken Name => ChildToken(1);

    /// <summary>左大括号</summary>
    public SyntaxToken OpenBrace => ChildToken(2);

    /// <summary>右大括号</summary>
    public SyntaxToken CloseBrace => LastToken();
}