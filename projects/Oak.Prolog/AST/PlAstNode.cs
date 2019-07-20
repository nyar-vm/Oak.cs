using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Prolog.AST;

public abstract record PlAstNode(TextSpan Span = default(TextSpan));
