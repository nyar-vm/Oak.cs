using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Typescript.AST;

#region 块与条件语句

/// <summary>
///     块语句
/// </summary>
public sealed record TsBlockStmt(IReadOnlyList<TsAstNode> Statements, TextSpan Span)
    : TsAstNode(Span)
{
    public TsBlockStmt(IReadOnlyList<TsAstNode> Statements)
        : this(Statements, default(TextSpan))
    {
    }
}

/// <summary>
///     if 条件语句
/// </summary>
public sealed record TsIfStmt(
    TsAstNode Condition,
    TsAstNode ThenBlock,
    TsAstNode? ElseBlock,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsIfStmt(TsAstNode Condition, TsAstNode ThenBlock, TsAstNode? ElseBlock)
        : this(Condition, ThenBlock, ElseBlock, default(TextSpan))
    {
    }
}

/// <summary>
///     switch 语句
/// </summary>
public sealed record TsSwitchStmt(
    TsAstNode Expression,
    IReadOnlyList<TsSwitchCase> Cases,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsSwitchStmt(TsAstNode Expression, IReadOnlyList<TsSwitchCase> Cases)
        : this(Expression, Cases, default(TextSpan))
    {
    }
}

/// <summary>
///     switch case 子句
/// </summary>
public sealed record TsSwitchCase(
    TsAstNode? Test,
    IReadOnlyList<TsAstNode> Statements,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsSwitchCase(TsAstNode? Test, IReadOnlyList<TsAstNode> Statements)
        : this(Test, Statements, default(TextSpan))
    {
    }
}

#endregion

#region 循环语句

/// <summary>
///     for 循环语句
/// </summary>
public sealed record TsForStmt(
    TsAstNode? Init,
    TsAstNode? Condition,
    TsAstNode? Increment,
    TsAstNode Body,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsForStmt(TsAstNode? Init, TsAstNode? Condition, TsAstNode? Increment, TsAstNode Body)
        : this(Init, Condition, Increment, Body, default(TextSpan))
    {
    }
}

/// <summary>
///     while 循环语句
/// </summary>
public sealed record TsWhileStmt(TsAstNode Condition, TsAstNode Body, TextSpan Span)
    : TsAstNode(Span)
{
    public TsWhileStmt(TsAstNode Condition, TsAstNode Body)
        : this(Condition, Body, default(TextSpan))
    {
    }
}

/// <summary>
///     do-while 循环语句
/// </summary>
public sealed record TsDoWhileStmt(TsAstNode Body, TsAstNode Condition, TextSpan Span)
    : TsAstNode(Span)
{
    public TsDoWhileStmt(TsAstNode Body, TsAstNode Condition)
        : this(Body, Condition, default(TextSpan))
    {
    }
}

/// <summary>
///     for-in 语句
/// </summary>
public sealed record TsForInStmt(
    TsAstNode Left,
    TsAstNode Right,
    TsAstNode Body,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsForInStmt(TsAstNode Left, TsAstNode Right, TsAstNode Body)
        : this(Left, Right, Body, default(TextSpan))
    {
    }
}

/// <summary>
///     for-of 语句
/// </summary>
public sealed record TsForOfStmt(
    TsAstNode Left,
    TsAstNode Right,
    TsAstNode Body,
    bool IsAwait,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsForOfStmt(TsAstNode Left, TsAstNode Right, TsAstNode Body, bool IsAwait)
        : this(Left, Right, Body, IsAwait, default(TextSpan))
    {
    }
}

#endregion

#region 跳转与控制语句

/// <summary>
///     return 语句
/// </summary>
public sealed record TsReturnStmt(TsAstNode? Value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsReturnStmt(TsAstNode? Value)
        : this(Value, default(TextSpan))
    {
    }
}

/// <summary>
///     throw 语句
/// </summary>
public sealed record TsThrowStmt(TsAstNode Value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsThrowStmt(TsAstNode Value)
        : this(Value, default(TextSpan))
    {
    }
}

/// <summary>
///     break 语句
/// </summary>
public sealed record TsBreakStmt(string? Label, TextSpan Span)
    : TsAstNode(Span)
{
    public TsBreakStmt(string? Label)
        : this(Label, default(TextSpan))
    {
    }
}

/// <summary>
///     continue 语句
/// </summary>
public sealed record TsContinueStmt(string? Label, TextSpan Span)
    : TsAstNode(Span)
{
    public TsContinueStmt(string? Label)
        : this(Label, default(TextSpan))
    {
    }
}

#endregion

#region 异常处理语句

/// <summary>
///     try-catch-finally 语句
/// </summary>
public sealed record TsTryStmt(
    TsAstNode Block,
    TsCatchClause? CatchClause,
    TsAstNode? FinallyBlock,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsTryStmt(TsAstNode Block, TsCatchClause? CatchClause, TsAstNode? FinallyBlock)
        : this(Block, CatchClause, FinallyBlock, default(TextSpan))
    {
    }
}

/// <summary>
///     catch 子句
/// </summary>
public sealed record TsCatchClause(
    string? ParameterName,
    TsAstNode? ParameterType,
    TsAstNode Block,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsCatchClause(string? ParameterName, TsAstNode? ParameterType, TsAstNode Block)
        : this(ParameterName, ParameterType, Block, default(TextSpan))
    {
    }
}

#endregion

#region 其他语句

/// <summary>
///     表达式语句
/// </summary>
public sealed record TsExprStmt(TsAstNode Expression, TextSpan Span)
    : TsAstNode(Span)
{
    public TsExprStmt(TsAstNode Expression)
        : this(Expression, default(TextSpan))
    {
    }
}

/// <summary>
///     debugger 语句
/// </summary>
public sealed record TsDebuggerStmt(TextSpan Span)
    : TsAstNode(Span)
{
    public TsDebuggerStmt()
        : this(default(TextSpan))
    {
    }
}

/// <summary>
///     空语句
/// </summary>
public sealed record TsEmptyStmt(TextSpan Span)
    : TsAstNode(Span)
{
    public TsEmptyStmt()
        : this(default(TextSpan))
    {
    }
}

/// <summary>
///     标签语句
/// </summary>
public sealed record TsLabeledStmt(string Label, TsAstNode Statement, TextSpan Span)
    : TsAstNode(Span)
{
    public TsLabeledStmt(string Label, TsAstNode Statement)
        : this(Label, Statement, default(TextSpan))
    {
    }
}

#endregion
