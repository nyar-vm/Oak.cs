using Oak.Syntax;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     Loop 遍历语句（类似 foreach）
/// </summary>
public sealed record LoopInStatement : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public LoopInStatement() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public LoopInStatement(string? iteratorName, ValkyrieNode? iterable, FunctionBody body, TextSpan span)
    {
        IteratorName = iteratorName;
        Iterable = iterable;
        Body = body;
        Span = span;
    }

    /// <summary>
    ///     迭代变量名称
    /// </summary>
    public string? IteratorName { get; init; }

    /// <summary>
    ///     被遍历的可迭代对象
    /// </summary>
    public ValkyrieNode? Iterable { get; init; }

    /// <summary>
    ///     循环体
    /// </summary>
    public FunctionBody Body { get; init; } = new();
}
