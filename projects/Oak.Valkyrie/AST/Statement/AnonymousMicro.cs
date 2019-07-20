using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     Lambda 表达式，匿名函数定义
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro() { ... }
/// micro(x, y) -> usize { x + y }
/// </code>
public sealed record AnonymousMicro : ValkyrieNode
{
    /// <summary>
    ///     参数列表（可省略类型标注）
    /// </summary>
    public IReadOnlyList<ParameterList> Parameters { get; init; } = [];

    /// <summary>
    ///     Lambda 体，可以是单个表达式或 <see cref="FunctionBody"/>
    /// </summary>
    public FunctionBody? Body { get; init; } = null;
}