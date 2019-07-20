using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Prolog.Lexer;

public sealed record PlToken(PlTokenType Type, string Value, int Line, int Column)
{
    public TextSpan ToSourceSpan()
    {
        return default;
    }
}
