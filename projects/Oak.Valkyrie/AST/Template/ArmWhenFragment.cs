using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     元信息声明，类或 Shader 中的 <c>meta</c> 配置段
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% when condition %>
/// </code>
public sealed record ArmWhenFragment : ValkyrieNode
{
    /// <summary>
    ///     元信息名称
    /// </summary>
    public TermNode Condition;

    public ArmWhenFragment(TermNode condition)
    {
        Condition = condition;
    }
}