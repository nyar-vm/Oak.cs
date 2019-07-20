using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     元信息键值对条目，用于 meta 块中的配置数据
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% config version = "1.0" %>
/// </code>
public sealed record MetaEntryDecl : ValkyrieNode
{
    /// <summary>
    ///     键名
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    ///     值表达式
    /// </summary>
    public ValkyrieNode Value { get; init; } = new TermAtomicLiteral();
}
