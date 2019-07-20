using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     if 片段节点，表示条件模板的起始标记
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% if condition %>
/// </code>
public sealed record IfFragment : ValkyrieNode
{
    TermNode Condition;

    public IfFragment(TermNode condition)
    {
        Condition = condition;
    }
}