using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     type 片段节点，表示类型模板的起始标记
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% type Typing %>
/// </code>
public sealed record ArmTypeFragment : ValkyrieNode
{
    /// <summary>
    ///     元信息名称
    /// </summary>
    public TypeNode Condition;
    public IReadOnlyList<ValkyrieNode> Body { get; }

    public ArmTypeFragment(TypeNode condition, IReadOnlyList<ValkyrieNode>? body = null)
    {
        Condition = condition;
        Body = body ?? [];
    }
}
