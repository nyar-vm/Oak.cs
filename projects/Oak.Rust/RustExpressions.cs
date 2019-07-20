using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Rust;

#region 表达式

/// <summary>
///     二元运算表达式
/// </summary>
public sealed record RustBinaryOp(RustAstNode Left, string Operator, RustAstNode Right, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     一元运算表达式
/// </summary>
public sealed record RustUnaryOp(string Operator, RustAstNode Operand, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     字面量表达式
/// </summary>
public sealed record RustLiteral(string Kind, string Value, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     标识符表达式
/// </summary>
public sealed record RustIdentifier(string Name, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     函数调用表达式
/// </summary>
public sealed record RustCall(RustAstNode Function, IReadOnlyList<RustAstNode> Arguments, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     方法调用表达式
/// </summary>
public sealed record RustMethodCall(
    RustAstNode Receiver,
    string Method,
    IReadOnlyList<RustAstNode> Arguments,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     字段访问表达式
/// </summary>
public sealed record RustFieldAccess(RustAstNode Object, string Field, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     下标访问表达式
/// </summary>
public sealed record RustIndex(RustAstNode Object, RustAstNode Index, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     类型转换表达式 (as)
/// </summary>
public sealed record RustCast(RustAstNode Expression, RustAstNode Type, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     范围表达式
/// </summary>
public sealed record RustRange(RustAstNode? Start, RustAstNode? End, bool Inclusive, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     闭包表达式
/// </summary>
public sealed record RustClosure(IReadOnlyList<string> Parameters, RustAstNode Body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     if 表达式
/// </summary>
public sealed record RustIfExpr(
    RustAstNode Condition,
    RustAstNode ThenBranch,
    RustAstNode? ElseBranch,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     match 表达式
/// </summary>
public sealed record RustMatchExpr(
    RustAstNode Scrutinee,
    IReadOnlyList<RustMatchArm> Arms,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     match 分支
/// </summary>
public sealed record RustMatchArm(RustAstNode Pattern, RustAstNode Body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     数组表达式
/// </summary>
public sealed record RustArrayExpr(IReadOnlyList<RustAstNode> Elements, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     元组表达式
/// </summary>
public sealed record RustTupleExpr(IReadOnlyList<RustAstNode> Elements, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     块表达式
/// </summary>
public sealed record RustBlockExpr(IReadOnlyList<RustAstNode> Statements, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     宏调用表达式
/// </summary>
public sealed record RustMacroCall(string Name, RustAstNode Body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

#endregion