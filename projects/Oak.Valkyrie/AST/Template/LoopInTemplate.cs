namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     元 loop_in 循环语句，在 meta 代码中进行迭代生成
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% loop_in field in expression %>
///     body
/// <% end loop_in %>
/// </code>
public sealed record LoopInTemplate : ValkyrieNode
{
    /// <summary>
    ///     带参数的构造函数
    /// </summary>
    /// <param name="variables">迭代变量名列表</param>
    /// <param name="collectionName">被迭代的集合名称</param>
    /// <param name="body">循环体 AST</param>
    public LoopInTemplate(IReadOnlyList<string> variables, string collectionName, IReadOnlyList<ValkyrieNode> body)
    {
        Variables = variables;
        CollectionName = collectionName;
        Body = body;
    }

    /// <summary>
    ///     迭代变量名列表
    /// </summary>
    public IReadOnlyList<string> Variables { get; }

    /// <summary>
    ///     被迭代的集合名称
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    ///     循环体代码 AST
    /// </summary>
    public IReadOnlyList<ValkyrieNode> Body { get; }
}
