using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Typescript.Lexer;

public sealed record TsToken(TsTokenType Type, string Value, int Line, int Column)
{
    public TextSpan ToSourceSpan()
    {
        return default;
    }
}
