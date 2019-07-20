using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.Lexer;

namespace Oak.Valkyrie.Parser;

/// <summary>
///     MSP 表达式解析的公开入口。
///     将 <c>&lt;% %&gt;</c> 内的 token 列表解析为 <see cref="ValkyrieNode"/> 表达式，
///     供外部 staging 引擎使用。
/// </summary>
public static class MetaExpressionApi
{
    /// <summary>
    ///     将指令体内的 token 列表解析为表达式 AST 节点。
    ///     例如 <c>arch</c> → <see cref="IdentifierNode"/>，
    ///     <c>target.spec == "windows"</c> → <see cref="TermBinaryExpression"/>。
    /// </summary>
    /// <param name="directiveTokens">指令体内的 token 列表（不含 TemplateL / TemplateR）</param>
    /// <returns>解析得到的表达式节点，解析失败返回 null</returns>
    public static ValkyrieNode? ParseDirectiveExpression(IEnumerable<GreenLeafNode> directiveTokens)
    {
        var tokenList = directiveTokens.ToList();
        if (tokenList.Count == 0)
        {
            return null;
        }

        // 若以关键字开头（match、if、case、loop 等），跳过首 token 解析其余部分
        var expressionTokens = tokenList;
        var firstKind = (ValkyrieTokenKind)tokenList[0].Kind.Value;
        if (IsDirectiveKeyword(firstKind))
        {
            expressionTokens = tokenList.Skip(1).ToList();
        }

        if (expressionTokens.Count == 0)
        {
            return new AST.Term.IdentifierNode("_");
        }

        var parserTokens = new List<GreenLeafNode>(expressionTokens.Count + 1);
        parserTokens.AddRange(expressionTokens);
        parserTokens.Add(new GreenLeafNode(ValkyrieTokenKind.Eos.ToNodeKind(), 0, string.Empty));

        var source = new TokenStream(parserTokens);
        return source.ParseExpressionNode();
    }

    private static bool IsDirectiveKeyword(ValkyrieTokenKind kind)
    {
        return kind is ValkyrieTokenKind.Match
            or ValkyrieTokenKind.If
            or ValkyrieTokenKind.Case
            or ValkyrieTokenKind.Loop
            or ValkyrieTokenKind.Else
            or ValkyrieTokenKind.End
            or ValkyrieTokenKind.Equal;
    }
}