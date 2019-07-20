using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.C;

/// <summary>
///     C 语言 AST 节点基类
/// </summary>
public abstract record CAstNode(TextSpan Span);