using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     Catch 错误处理语句，捕获表达式可能抛出的错误并按模式匹配处理
/// </summary>
/// <para>示例：</para>
/// <code>
/// catch parse_json(data)
/// | JsonError { msg } => { log(msg); null }
/// | _ => { null }
/// </code>
public sealed record CatchStatement : ValkyrieNode
{
    /// <summary>
    ///     被监视的表达式
    /// </summary>
    public ValkyrieNode Expression { get; init; } = new IdentifierNode();

    /// <summary>
    ///     错误处理分支列表
    /// </summary>
    public IReadOnlyList<CatchArm> Arms { get; init; } = [];
}
