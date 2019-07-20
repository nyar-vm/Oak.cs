using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.OCaml.AST;

public abstract record OcAstNode(TextSpan Span = default(TextSpan));
