namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     case 分支片段节点，表示匹配分支的起始标记
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% case pattern %>
/// </code>
public sealed record ArmCaseFragment : ValkyrieNode
{
}
