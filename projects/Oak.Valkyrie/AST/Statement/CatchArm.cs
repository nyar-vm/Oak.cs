using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     Catch 分支，匹配特定错误类型的处理臂
/// </summary>
/// <para>示例：</para>
/// <code>
/// catch risky_operation()
/// | IOError => { log("文件错误"); }
/// | ParseError => { log("解析错误"); }
/// </code>
public sealed record CatchArm : ValkyrieNode
{
    /// <summary>
    ///     匹配的错误模式
    /// </summary>
    public ValkyrieNode Pattern { get; init; } = new IdentifierNode();

    /// <summary>
    ///     错误处理代码体
    /// </summary>
    public FunctionBody Body { get; init; } = new();
}
