using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Haskell.Lexer;

public sealed record HsToken(HsTokenType Type, string Value, int Line, int Column)
{
    public TextSpan ToSourceSpan()
    {
        return default;
    }
}
