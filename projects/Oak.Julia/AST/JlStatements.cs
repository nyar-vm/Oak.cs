using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Julia.AST;

#region 语句

public sealed record JlIfExpr(
    JlAstNode Condition,
    JlAstNode ThenBranch,
    IReadOnlyList<(JlAstNode Condition, JlAstNode Body)> ElseIfBranches,
    JlAstNode? ElseBranch,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlForExpr(
    IReadOnlyList<(JlAstNode Iterator, JlAstNode Iterable)> Iterators,
    JlAstNode Body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlWhileExpr(
    JlAstNode Condition,
    JlAstNode Body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTryExpr(
    JlAstNode Body,
    IReadOnlyList<(JlAstNode Pattern, JlAstNode Body)> CatchClauses,
    JlAstNode? FinallyBody,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlLetExpr(
    IReadOnlyList<JlAstNode> Bindings,
    JlAstNode Body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlDoBlock(
    JlAstNode Call,
    IReadOnlyList<JlAstNode> Parameters,
    JlAstNode Body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlReturnExpr(
    JlAstNode? Value,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlBreakExpr(TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlContinueExpr(TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlBlock(
    IReadOnlyList<JlAstNode> Statements,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlAssignment(
    JlAstNode Left,
    JlAstNode Right,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlCompoundAssignment(
    JlAstNode Left,
    string Operator,
    JlAstNode Right,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion
