namespace Oak.Syntax;

/// <summary>
///     语法树工厂，从源文本创建语法树
/// </summary>
public sealed class SyntaxTreeFactory
{
    private readonly Language _language;
    private readonly Func<ISource, Language, GreenNode> _parseGreen;

    /// <summary>
    ///     初始化语法树工厂
    /// </summary>
    /// <param name="language">语言配置</param>
    /// <param name="parseGreen">解析器委托，从源文本产出绿树根节点</param>
    public SyntaxTreeFactory(Language language, Func<ISource, Language, GreenNode> parseGreen)
    {
        _language = language;
        _parseGreen = parseGreen;
    }

    /// <summary>
    ///     从源文本创建语法树
    /// </summary>
    /// <param name="source">源文本</param>
    /// <returns>语法树</returns>
    public SyntaxTree CreateTree(ISource source)
    {
        var green = _parseGreen(source, _language);
        return new SyntaxTree(source, green);
    }

    /// <summary>
    ///     从字符串创建语法树
    /// </summary>
    /// <param name="text">源代码文本</param>
    /// <returns>语法树</returns>
    public SyntaxTree CreateTree(string text)
    {
        return CreateTree(new StringSource(text));
    }
}

/// <summary>
///     强类型 AST 节点工厂基类，提供程序化构造语法节点的通用能力
/// </summary>
/// <typeparam name="TRoot">语言特定的语法根类型</typeparam>
public abstract class SyntaxFactory<TRoot> where TRoot : SyntaxRoot
{
    /// <summary>
    ///     从绿树节点和语法树信息构造强类型语法根
    /// </summary>
    /// <param name="green">绿树根节点</param>
    /// <param name="tree">所属语法树</param>
    /// <returns>强类型语法根</returns>
    public abstract TRoot CreateRoot(GreenNode green, SyntaxTree tree);

    /// <summary>
    ///     使用 CstBuilder 程序化构造语法根
    /// </summary>
    /// <param name="buildAction">构建动作，在构建器上执行操作</param>
    /// <returns>强类型语法根（无关联语法树）</returns>
    public TRoot BuildRoot(Action<CstBuilder> buildAction)
    {
        var b = new CstBuilder();
        buildAction(b);
        var green = b.Build();
        return CreateRoot(green, null!);
    }
}