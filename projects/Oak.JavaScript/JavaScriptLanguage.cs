using Oak.JavaScript.Lexer;
using Oak.JavaScript.Parser;
using Oak.Syntax;
using Oak.Typescript.AST;

namespace Oak.JavaScript;

/// <summary>
///     JavaScript 语言前端，封装词法分析、语法分析管线
///     复用 Oak.Typescript 基础设施，JavaScript 是 TypeScript 的子集
/// </summary>
public sealed class JavaScriptLanguage : Language
{
    private readonly JsLexer _lexer = new();
    private readonly JsParser _parser = new();

    public override string Name => "JavaScript";

    /// <summary>
    ///     将 JavaScript 源码解析为 AST
    /// </summary>
    /// <param name="source">JavaScript 源代码</param>
    /// <returns>AST 根节点（TsCompilationUnit）</returns>
    public TsAstNode Parse(string source)
    {
        var tokens = _lexer.Tokenize(source);
        return _parser.Parse(tokens);
    }
}