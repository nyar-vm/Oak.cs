using Oak.Parsing;

namespace Oak.Rust;

/// <summary>
///     Rust 语言解析管道（词法 + 语法）
/// </summary>
public sealed class RustPipeline : IStringParser<RustAstNode>
{
    private readonly RustLexer _lexer;
    private readonly RustParser _parser;

    /// <summary>
    ///     创建 Rust 解析管道
    /// </summary>
    public RustPipeline()
    {
        _lexer = new RustLexer();
        _parser = new RustParser();
    }

    /// <summary>
    ///     解析 Rust 源代码为 AST
    /// </summary>
    public RustAstNode Parse(string source)
    {
        var tokens = _lexer.TokenizeAsRustTokens(source);
        return _parser.Parse(tokens);
    }
}