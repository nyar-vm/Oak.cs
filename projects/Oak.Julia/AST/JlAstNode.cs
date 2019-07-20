using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Julia.AST;

public abstract record JlAstNode(TextSpan Span = default(TextSpan));
