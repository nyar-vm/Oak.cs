using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Julia.Lexer;

public sealed record JlToken(JlTokenType Type, string Value, int Line, int Column)
{
    public TextSpan ToSourceSpan()
    {
        return default;
    }
}
