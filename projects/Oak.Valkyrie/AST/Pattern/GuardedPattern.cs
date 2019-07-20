using Oak.Syntax;

namespace Oak.Valkyrie.AST.Pattern;

/// <summary>
///     带守卫条件的模式 <c>case x if x > 0:</c>
///     在模式匹配成功后额外检查守卫条件
/// </summary>
public sealed record GuardedPattern : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public GuardedPattern() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public GuardedPattern(ValkyrieNode pattern, ValkyrieNode guard, TextSpan span)
    {
        Pattern = pattern;
        Guard = guard;
        Span = span;
    }

    /// <summary>
    ///     内层模式
    /// </summary>
    public ValkyrieNode Pattern { get; init; } = default!;

    /// <summary>
    ///     守卫条件表达式
    /// </summary>
    public ValkyrieNode Guard { get; init; } = default!;

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType Type => ValkyrieNodeType.GuardedPattern;
}
