using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     Shader 声明语法节点：shader Name { ... }
/// </summary>
public sealed class ShaderSyntax : ValkyrieDeclarationSyntax
{
    public ShaderSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>shader 关键字</summary>
    public SyntaxToken ShaderKeyword => ChildToken(0);

    /// <summary>Shader 名称</summary>
    public SyntaxToken Name => ChildToken(1);

    /// <summary>左大括号</summary>
    public SyntaxToken OpenBrace => ChildToken(2);

    /// <summary>右大括号</summary>
    public SyntaxToken CloseBrace => LastToken();
}