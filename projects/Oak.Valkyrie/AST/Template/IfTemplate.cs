namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     元 if 条件语句，在 meta 代码中进行条件编译
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% if conditionExpr %>
///     then code...
/// <% else %>
///     else code...
/// <% end if %>
/// </code>
public sealed record IfTemplate : ValkyrieNode
{
    /// <summary>
    ///     带参数的构造函数
    /// </summary>
    /// <param name="condition">条件表达式 AST</param>
    /// <param name="thenBody">条件为真的代码体 AST</param>
    /// <param name="elseBody">条件为假的代码体 AST，为 <c>null</c> 时无 else</param>
    public IfTemplate(ValkyrieNode condition, IReadOnlyList<ValkyrieNode> thenBody, IReadOnlyList<ValkyrieNode>? elseBody)
    {
        Condition = condition;
        ThenBody = thenBody;
        ElseBody = elseBody;
    }

    /// <summary>
    ///     条件表达式 AST
    /// </summary>
    public ValkyrieNode Condition { get; }

    /// <summary>
    ///     条件为真时执行的代码体 AST
    /// </summary>
    public IReadOnlyList<ValkyrieNode> ThenBody { get; }

    /// <summary>
    ///     else 分支代码体 AST，为 <c>null</c> 时表示无 else 分支
    /// </summary>
    public IReadOnlyList<ValkyrieNode>? ElseBody { get; }
}
