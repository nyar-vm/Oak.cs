namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     loop 片段节点，表示循环模板的起始标记
/// </summary>
public sealed record LoopFragment : ValkyrieNode
{
    /// <summary>
    ///     loop 片段节点，表示循环模板的起始标记
    /// </summary>
    /// <para>示例：</para>
    /// <code>
    /// <% loop pattern in expression %>
    /// </code>
}