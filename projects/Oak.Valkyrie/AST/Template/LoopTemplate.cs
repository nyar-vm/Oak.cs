namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     元 for 循环语句，在 meta 代码中进行数字范围迭代
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% loop pattern in expression %>
///     body
/// <% end loop %>
/// </code>
public sealed record LoopTemplate : ValkyrieNode
{
    /// <summary>
    ///     带参数的构造函数
    /// </summary>
    /// <param name="variableName">循环变量名</param>
    /// <param name="rangeStart">范围起始值</param>
    /// <param name="rangeEnd">范围结束值</param>
    /// <param name="body">循环体 AST</param>
    public LoopTemplate(string variableName, string rangeStart, string rangeEnd, IReadOnlyList<ValkyrieNode> body)
    {
        VariableName = variableName;
        RangeStart = rangeStart;
        RangeEnd = rangeEnd;
        Body = body;
    }

    /// <summary>
    ///     循环变量名
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    ///     范围起始值
    /// </summary>
    public string RangeStart { get; }

    /// <summary>
    ///     范围结束值
    /// </summary>
    public string RangeEnd { get; }

    /// <summary>
    ///     循环体代码 AST
    /// </summary>
    public IReadOnlyList<ValkyrieNode> Body { get; }
}
