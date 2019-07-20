using Oak.Typescript.Lexer;

namespace Oak.JavaScript.Lexer;

public sealed class JsLexer
{
    private readonly TsLexer _tsLexer = new();

    public IReadOnlyList<TsToken> Tokenize(string source)
    {
        return _tsLexer.Tokenize(source);
    }
}
