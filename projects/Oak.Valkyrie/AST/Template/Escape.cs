namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     MSP 转义节点：<c>$(var)</c>。
///     在 bracketed code 中引用外层 staging level 的变量，
///     将变量值"下放"到当前 level。
///     对应 MetaOCaml 的 cross-stage persistence / escape。
/// </summary>
public sealed record Escape : ValkyrieNode
{
    /// <summary>
    ///     初始化转义节点。
    /// </summary>
    /// <param name="variableName">外层 staging level 中的变量名</param>
    public Escape(string variableName)
    {
        VariableName = variableName;
    }

    /// <summary>
    ///     外层 staging level 中的变量名（不含 <c>$</c> 和括号）
    /// </summary>
    public string VariableName { get; }
}