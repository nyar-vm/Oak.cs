using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Haskell.AST;

public abstract record HsAstNode(TextSpan Span = default(TextSpan));
