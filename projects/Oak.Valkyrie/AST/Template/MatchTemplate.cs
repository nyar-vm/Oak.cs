namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     元 match 语句，meta 代码中的多分支匹配选择
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% match expr %>
///     <% case pattern1 %>
///         body1
///     <% case pattern2 %>
///         body2
///     <% else %>
///         default
/// <% end match %>
/// </code>
public sealed record MatchTemplate : ValkyrieNode
{
    /// <summary>
    ///     带参数的构造函数
    /// </summary>
    /// <param name="value">被匹配值表达式 AST</param>
    /// <param name="arms">匹配分支列表</param>
    /// <param name="defaultBody">默认分支代码体 AST，为 <c>null</c> 时无默认分支</param>
    public MatchTemplate(ValkyrieNode value, IReadOnlyList<ArmTypeFragment> arms, IReadOnlyList<ValkyrieNode>? defaultBody)
    {
        Value = value;
        Arms = arms;
        DefaultBody = defaultBody;
    }

    /// <summary>
    ///     被匹配的值表达式 AST
    /// </summary>
    public ValkyrieNode Value { get; }

    /// <summary>
    ///     匹配分支列表
    /// </summary>
    public IReadOnlyList<ArmTypeFragment> Arms { get; }

    /// <summary>
    ///     默认分支代码体 AST，为 <c>null</c> 时表示无默认分支
    /// </summary>
    public IReadOnlyList<ValkyrieNode>? DefaultBody { get; }
}
