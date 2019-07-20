using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     导入声明语法节点：import "path"
/// </summary>
public sealed class ImportSyntax : ValkyrieDeclarationSyntax
{
    public ImportSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>import 关键字</summary>
    public SyntaxToken ImportKeyword => ChildToken(0);

    /// <summary>导入路径字符串</summary>
    public SyntaxToken Path => ChildToken(1);
}