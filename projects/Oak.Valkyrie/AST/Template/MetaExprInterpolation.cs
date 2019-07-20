namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     元表达式插值节点，表示 meta 代码中的 <c>{{ expression }}</c> 插值
/// </summary>
/// <para>示例：</para>
/// <code>
/// {{ expression }}
/// </code>
public sealed record MetaExprInterpolation : ValkyrieNode
{
    /// <summary>
    ///     带表达式的构造函数
    /// </summary>
    /// <param name="expression">插值表达式文本</param>
    public MetaExprInterpolation(string expression)
    {
        Expression = expression;
    }

    /// <summary>
    ///     插值表达式内容
    /// </summary>
    public string Expression { get; }
}
