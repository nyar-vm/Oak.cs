using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Rust;

/// <summary>
///     Rust 语言 AST 节点基类
/// </summary>
public abstract record RustAstNode(TextSpan Span);