using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     服务声明语法节点：service Name { ... }
/// </summary>
public sealed class ServiceSyntax : ValkyrieDeclarationSyntax
{
    public ServiceSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>service 关键字</summary>
    public SyntaxToken ServiceKeyword => ChildToken(0);

    /// <summary>服务名称</summary>
    public SyntaxToken Name => ChildToken(1);

    /// <summary>左大括号</summary>
    public SyntaxToken OpenBrace => ChildToken(2);

    /// <summary>右大括号</summary>
    public SyntaxToken CloseBrace => LastToken();
}