using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Python.AST;

#region 表达式

/// <summary>
///     二元运算表达式
/// </summary>
public sealed record PyBinaryOp(PyAstNode Left, string Operator, PyAstNode Right, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     一元运算表达式
/// </summary>
public sealed record PyUnaryOp(string Operator, PyAstNode Operand, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     字面量表达式
/// </summary>
public sealed record PyLiteral(string Kind, string Value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     标识符表达式
/// </summary>
public sealed record PyIdentifier(string Name, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     函数调用表达式
/// </summary>
public sealed record PyCall(PyAstNode Function, IReadOnlyList<PyAstNode> Arguments, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     属性访问表达式
/// </summary>
public sealed record PyAttribute(PyAstNode Object, string Name, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     下标访问表达式
/// </summary>
public sealed record PySubscript(PyAstNode Object, PyAstNode Index, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     列表字面量
/// </summary>
public sealed record PyList(IReadOnlyList<PyAstNode> Elements, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     元组字面量
/// </summary>
public sealed record PyTuple(IReadOnlyList<PyAstNode> Elements, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     字典字面量
/// </summary>
public sealed record PyDict(IReadOnlyList<(PyAstNode Key, PyAstNode Value)> Items, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Lambda 表达式
/// </summary>
public sealed record PyLambda(IReadOnlyList<string> Parameters, PyAstNode Body, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

#endregion