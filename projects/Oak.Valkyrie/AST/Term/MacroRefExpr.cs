namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     宏引用表达式，表示 <c>$macro_name</c> 形式的宏调用
/// </summary>
/// <para>示例：</para>
/// <code>
/// meta {
///     $include("header.ggs");
///     var version = $VERSION;
/// }
/// </code>
public sealed record MacroRefExpr : ValkyrieNode
{
    /// <summary>
    ///     带名称和前缀标记的构造函数
    /// </summary>
    /// <param name="name">宏名称</param>
    /// <param name="isDollarPrefix">是否以 <c>$</c> 为前缀</param>
    public MacroRefExpr(string name, bool isDollarPrefix)
    {
        Name = name;
        IsDollarPrefix = isDollarPrefix;
    }

    /// <summary>
    ///     宏名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     是否以 <c>$</c> 为前缀
    /// </summary>
    public bool IsDollarPrefix { get; }
}
