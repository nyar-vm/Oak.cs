using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Python.AST;

/// <summary>
///     Python AST 节点基类
/// </summary>
public abstract record PyAstNode(TextSpan Span = default(TextSpan));
