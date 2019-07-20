namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     MSP 插值节点：<c>&lt;%= expr %&gt;</c>。
///     在 staging 环境中求值表达式，生成 AST 片段，
///     嵌入到当前 level 的程序结构中。
///     对应 MetaOCaml 的 escape/splice 操作符。
/// </summary>
public sealed record Splice : ValkyrieNode
{
    /// <summary>
    ///     初始化插值节点。
    /// </summary>
    /// <param name="expression">要被求值并嵌入的表达式</param>
    public Splice(ValkyrieNode expression)
    {
        Expression = expression;
    }

    /// <summary>
    ///     待求值并嵌入的表达式 AST
    /// </summary>
    public ValkyrieNode Expression { get; }
}