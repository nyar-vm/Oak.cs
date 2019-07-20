using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.C;

#region 表达式

/// <summary>
///     二元运算表达式
/// </summary>
public sealed record CBinaryOp(CAstNode Left, string Operator, CAstNode Right, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     一元运算表达式
/// </summary>
public sealed record CUnaryOp(string Operator, CAstNode Operand, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     三元条件表达式
/// </summary>
public sealed record CTernaryOp(CAstNode Condition, CAstNode ThenExpr, CAstNode ElseExpr, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     字面量表达式
/// </summary>
public sealed record CLiteral(string Kind, string Value, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     标识符表达式
/// </summary>
public sealed record CIdentifier(string Name, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     函数调用表达式
/// </summary>
public sealed record CCall(CAstNode Function, IReadOnlyList<CAstNode> Arguments, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     成员访问表达式
/// </summary>
public sealed record CMemberAccess(CAstNode Object, string Member, bool IsPointer, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     下标访问表达式
/// </summary>
public sealed record CSubscript(CAstNode Object, CAstNode Index, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     类型转换表达式
/// </summary>
public sealed record CCast(CAstNode Type, CAstNode Expression, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     sizeof 表达式
/// </summary>
public sealed record CSizeOf(CAstNode Operand, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     数组初始化列表
/// </summary>
public sealed record CInitList(IReadOnlyList<CAstNode> Elements, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

#endregion