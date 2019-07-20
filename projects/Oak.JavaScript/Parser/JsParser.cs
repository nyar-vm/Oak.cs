using Oak.Diagnostics;
using Oak.Typescript.AST;
using Oak.Typescript.Lexer;
using Oak.Typescript.Parsing;

namespace Oak.JavaScript.Parser;

public sealed class JsParser
{
    private readonly TsParser _tsParser = new();

    public JsParser(DiagnosticSink? diagnostics = null)
    {
        _tsParser = new TsParser(diagnostics);
    }

    public TsAstNode Parse(IReadOnlyList<TsToken> tokens)
    {
        return _tsParser.Parse(tokens);
    }
}
