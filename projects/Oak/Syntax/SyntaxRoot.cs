namespace Oak.Syntax;

/// <summary>
///     语言顶层结构的基类
/// </summary>
public abstract class SyntaxRoot : SyntaxNode
{
    /// <summary>
    ///     初始化语法根节点
    /// </summary>
    protected SyntaxRoot(GreenNode green, SyntaxTree tree, int offset, string languageId)
        : base(green, tree, offset)
    {
        LanguageId = languageId;
    }

    /// <summary>
    ///     语言标识符
    /// </summary>
    public string LanguageId { get; }
}