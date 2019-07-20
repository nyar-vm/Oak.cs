using Oak.Syntax;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Statement;

/// <summary>
///     If 条件语句，支持以下语法形式：
///     <list type="bullet">
///         <item><c>if &lt;EXPR&gt; { ... }</c> — 仅 then 分支</item>
///         <item><c>if &lt;EXPR&gt; { ... } else { ... }</c> — then + else 分支</item>
///         <item><c>if &lt;EXPR&gt; { ... } else if &lt;EXPR&gt; { ... }</c> — else if 链（<c>ElseBlock</c> 嵌套 <c>IfStatement</c>）</item>
///         <item><c>if &lt;EXPR&gt; { ... } else if &lt;EXPR&gt; { ... } else { ... }</c> — else if 链 + 最终 else</item>
///     </list>
/// </summary>
/// <remarks>
///     <para>示例：</para>
///     <code>
/// if x &gt; 0 {
///     print("正数");
/// }
/// else if x &lt; 0 {
///     print("负数");
/// }
/// else {
///     print("零");
/// }
///     </code>
/// </remarks>
public sealed record IfStatement : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public IfStatement() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public IfStatement(ValkyrieNode condition, FunctionBody thenBlock, ValkyrieNode? elseBlock, TextSpan span)
    {
        Condition = condition;
        ThenBlock = thenBlock;
        ElseBlock = elseBlock;
        Span = span;
    }

    public override ValkyrieNodeType Type => ValkyrieNodeType.IfStmt;

    /// <summary>
    ///     条件表达式
    /// </summary>
    public ValkyrieNode Condition { get; init; } = new IdentifierNode();

    /// <summary>
    ///     Then 分支代码块，条件为真时执行
    /// </summary>
    public FunctionBody ThenBlock { get; init; } = new();

    /// <summary>
    ///     Else 分支，可以是 <c>BlockStmt</c>（最终 else）或嵌套的 <c>IfStatement</c>（else if 链）。
    ///     为 <c>null</c> 时表示仅有 then 分支
    /// </summary>
    public ValkyrieNode? ElseBlock { get; init; }
}
