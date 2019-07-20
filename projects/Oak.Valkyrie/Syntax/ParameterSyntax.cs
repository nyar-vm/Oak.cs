using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     参数声明语法节点：Name: Type
/// </summary>
public sealed class ParameterSyntax : ValkyrieDeclarationSyntax
{
    public ParameterSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>参数名称</summary>
    public SyntaxToken Name => ChildToken(0);

    /// <summary>冒号</summary>
    public SyntaxToken Colon => ChildToken(1);

    /// <summary>类型名称</summary>
    public SyntaxToken TypeName => ChildToken(2);
}