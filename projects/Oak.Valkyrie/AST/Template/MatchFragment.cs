using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     match 片段节点，表示匹配模板的起始标记
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% match expression %>
/// </code>
public sealed record MatchFragment : ValkyrieNode
{
    
    TermNode Expression;

    public MatchFragment(TermNode expression)
    {
        Expression = expression;
    }
}