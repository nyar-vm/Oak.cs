using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Erlang.AST;

public abstract record ErAstNode(TextSpan Span = default(TextSpan));
