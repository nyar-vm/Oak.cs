using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Python.AST;

#region 语句

/// <summary>
///     赋值语句
/// </summary>
public sealed record PyAssign(PyAstNode Target, PyAstNode Value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     增量赋值语句（如 x += 1）
/// </summary>
public sealed record PyAugAssign(PyAstNode Target, string Operator, PyAstNode Value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     表达式语句
/// </summary>
public sealed record PyExprStmt(PyAstNode Expression, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     If 语句
/// </summary>
public sealed record PyIf(
    PyAstNode Condition,
    IReadOnlyList<PyAstNode> ThenBody,
    IReadOnlyList<PyAstNode>? ElseBody,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     While 循环语句
/// </summary>
public sealed record PyWhile(
    PyAstNode Condition,
    IReadOnlyList<PyAstNode> Body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     For 循环语句
/// </summary>
public sealed record PyFor(
    string Iterator,
    PyAstNode Iterable,
    IReadOnlyList<PyAstNode> Body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Return 语句
/// </summary>
public sealed record PyReturn(PyAstNode? Value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Yield 语句（生成器）
/// </summary>
public sealed record PyYield(PyAstNode? Value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Break 语句
/// </summary>
public sealed record PyBreak(TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Continue 语句
/// </summary>
public sealed record PyContinue(TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     函数定义
/// </summary>
public sealed record PyFunctionDef(
    string Name,
    IReadOnlyList<string> Parameters,
    IReadOnlyList<PyAstNode> Body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     类定义
/// </summary>
public sealed record PyClassDef(
    string Name,
    IReadOnlyList<PyAstNode> Body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Try/Except 异常处理语句
/// </summary>
public sealed record PyTry(
    IReadOnlyList<PyAstNode> Body,
    IReadOnlyList<PyExceptClause> Handlers,
    IReadOnlyList<PyAstNode>? ElseBody,
    IReadOnlyList<PyAstNode>? FinallyBody,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Except 子句
/// </summary>
public sealed record PyExceptClause(
    PyAstNode? ExceptionType,
    string? Name,
    IReadOnlyList<PyAstNode> Body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Raise 语句
/// </summary>
public sealed record PyRaise(PyAstNode? Exception, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Import 语句
/// </summary>
public sealed record PyImport(IReadOnlyList<PyImportItem> Items, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     From...Import 语句
/// </summary>
public sealed record PyFromImport(string Module, IReadOnlyList<PyImportItem> Items, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Import 项
/// </summary>
public sealed record PyImportItem(string Name, string? Alias, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     Pass 语句
/// </summary>
public sealed record PyPass(TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

/// <summary>
///     模块（根节点）
/// </summary>
public sealed record PyModule(IReadOnlyList<PyAstNode> Body, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

#endregion