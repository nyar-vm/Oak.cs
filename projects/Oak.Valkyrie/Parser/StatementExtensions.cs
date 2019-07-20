using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Pattern;
using Oak.Valkyrie.AST.Statement;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Template;
using Oak.Valkyrie.Lexer;

namespace Oak.Valkyrie.Parser;

/// <summary>
/// 语句解析扩展入口。
/// </summary>
internal static class StatementExtensions
{
    internal static ValkyrieNode ParseStatementNode(this TokenStream tokens, ValkyrieLanguage language)
    {
        if (tokens.Check(ValkyrieTokenKind.Return))
        {
            tokens.Advance();
            ValkyrieNode? value = null;
            if (!tokens.Check(ValkyrieTokenKind.Semicolon) && !tokens.Check(ValkyrieTokenKind.BraceR))
            {
                value = tokens.ParseExpressionNode();
            }

            ExpectOrWarnSemicolon(tokens);
            return new ReturnStatement { Value = value };
        }

        if (tokens.Check(ValkyrieTokenKind.Resume))
        {
            return ParseResumeStmt(tokens);
        }

        if (tokens.Check(ValkyrieTokenKind.Let))
        {
            tokens.Advance();
            return tokens.ParseVariableDeclNode(language);
        }

        if (tokens.Check(ValkyrieTokenKind.If))
        {
            return ParseIfStmt(tokens, language);
        }

        if (tokens.Check(ValkyrieTokenKind.Match))
        {
            return ParseMatchStmt(tokens, language);
        }

        if (tokens.Check(ValkyrieTokenKind.Catch))
        {
            return ParseCatchStmt(tokens, language);
        }

        if (tokens.Check(ValkyrieTokenKind.While))
        {
            return ParseWhileStmt(tokens, language);
        }

        if (tokens.Check(ValkyrieTokenKind.Loop))
        {
            return ParseLoopStmt(tokens, language);
        }

        if (tokens.Check(ValkyrieTokenKind.Discard))
        {
            tokens.Advance();
            ExpectOrWarnSemicolon(tokens);
            return new LetStatement();
        }

        if (tokens.Check(ValkyrieTokenKind.TemplateL))
        {
            var metaNode = tokens.ParseMetaTemplateNode();
            return metaNode ?? new MetaTemplateText(string.Empty);
        }

        if (tokens.Check(ValkyrieTokenKind.BraceL))
        {
            return tokens.ParseBlockNode(language);
        }

        if (tokens.IsDeclarationStartNode(language))
        {
            return tokens.ParseDeclarationNode(language);
        }

        var expr = tokens.ParseExpressionNode();
        ExpectOrWarnSemicolon(tokens);
        return new TermNode { Expression = expr };
    }

    private static IfStatement ParseIfStmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.Advance();
        tokens.Expect(ValkyrieTokenKind.ParenthesisL);
        var cond = tokens.ParseExpressionNode();
        tokens.Expect(ValkyrieTokenKind.ParenthesisR);

        var thenBlock = tokens.ParseBlockNode(language);
        ValkyrieNode? elseBlock = null;

        if (tokens.Check(ValkyrieTokenKind.Else))
        {
            tokens.Advance();
            if (tokens.Check(ValkyrieTokenKind.If))
            {
                elseBlock = ParseIfStmt(tokens, language);
            }
            else
            {
                elseBlock = tokens.ParseBlockNode(language);
            }
        }

        return new IfStatement(cond, thenBlock, elseBlock, default);
    }

    private static MatchStatement ParseMatchStmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.Advance();
        var expr = tokens.ParseExpressionNode();

        tokens.Expect(ValkyrieTokenKind.BraceL);
        var arms = new List<MatchArm>();
        while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
        {
            arms.Add(ParseMatchArm(tokens, language));
        }

        tokens.Expect(ValkyrieTokenKind.BraceR);
        return new MatchStatement { Expression = expr, Arms = arms };
    }

    private static MatchArm ParseMatchArm(TokenStream tokens, ValkyrieLanguage language)
    {
        ValkyrieNode pattern;
        if (tokens.Check(ValkyrieTokenKind.Case))
        {
            tokens.Advance();
            pattern = ParseArmPattern(tokens);
        }
        else if (tokens.Check(ValkyrieTokenKind.Else))
        {
            tokens.Advance();
            pattern = new IdentifierNode { Name = "else" };
        }
        else
        {
            pattern = tokens.ParseExpressionNode();
        }

        tokens.Expect(ValkyrieTokenKind.Colon);
        var body = ParseArmBody(tokens, language);
        return new MatchArm { Pattern = pattern, Body = body };
    }

    private static CatchStatement ParseCatchStmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.Advance();
        var expr = tokens.ParseExpressionNode();

        tokens.Expect(ValkyrieTokenKind.BraceL);
        var arms = new List<CatchArm>();
        while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
        {
            arms.Add(ParseCatchArm(tokens, language));
        }

        tokens.Expect(ValkyrieTokenKind.BraceR);
        return new CatchStatement { Expression = expr, Arms = arms };
    }

    private static CatchArm ParseCatchArm(TokenStream tokens, ValkyrieLanguage language)
    {
        ValkyrieNode pattern;
        if (tokens.Check(ValkyrieTokenKind.Case))
        {
            tokens.Advance();
            pattern = ParseArmPattern(tokens);
        }
        else if (tokens.Check(ValkyrieTokenKind.Else))
        {
            tokens.Advance();
            pattern = new IdentifierNode { Name = "else" };
        }
        else
        {
            pattern = tokens.ParseExpressionNode();
        }

        tokens.Expect(ValkyrieTokenKind.Colon);
        var body = ParseArmBody(tokens, language);
        return new CatchArm { Pattern = pattern, Body = body };
    }

    private static FunctionBody ParseArmBody(TokenStream tokens, ValkyrieLanguage language)
    {
        if (tokens.Check(ValkyrieTokenKind.BraceL))
        {
            return tokens.ParseBlockNode(language);
        }

        var stmts = new List<ValkyrieNode> { tokens.ParseStatementNode(language) };
        while (!tokens.IsAtEnd()
               && !tokens.Check(ValkyrieTokenKind.BraceR)
               && !tokens.Check(ValkyrieTokenKind.Case)
               && !tokens.Check(ValkyrieTokenKind.Else))
        {
            stmts.Add(tokens.ParseStatementNode(language));
        }

        return new FunctionBody { Statements = stmts };
    }

    private static ValkyrieNode ParseArmPattern(TokenStream tokens)
    {
        if (tokens.IsLiteral() || tokens.Check(ValkyrieTokenKind.Number) || tokens.Check(ValkyrieTokenKind.String))
        {
            var expr = tokens.ParseExpressionNode();
            if (expr is TermAtomicLiteral literal)
            {
                return new ConstantPattern { Value = literal };
            }

            return expr;
        }

        if (tokens.Check(ValkyrieTokenKind.Identifier) && tokens.PeekText() == "_"
            && (tokens.PeekKind(1) == ValkyrieTokenKind.Colon || tokens.PeekKind(1) == ValkyrieTokenKind.BraceR))
        {
            tokens.Advance();
            return new WildcardPattern();
        }

        var name = tokens.AdvanceText();
        if (tokens.Check(ValkyrieTokenKind.ParenthesisL))
        {
            tokens.Advance();
            ValkyrieNode? inner = null;
            if (!tokens.Check(ValkyrieTokenKind.ParenthesisR))
            {
                inner = tokens.ParseExpressionNode();
            }

            tokens.Expect(ValkyrieTokenKind.ParenthesisR);
            return new TermCallExpression
            {
                Callee = new IdentifierNode { Name = name },
                Arguments = inner != null ? new List<ValkyrieNode> { inner } : Array.Empty<ValkyrieNode>()
            };
        }

        return new DeclarationPattern { Name = name };
    }

    private static ResumeStatement ParseResumeStmt(TokenStream tokens)
    {
        tokens.Advance();
        ValkyrieNode? value = null;
        if (!tokens.Check(ValkyrieTokenKind.Semicolon) && !tokens.Check(ValkyrieTokenKind.BraceR))
        {
            value = tokens.ParseExpressionNode();
        }

        ExpectOrWarnSemicolon(tokens);
        return new ResumeStatement { Value = value };
    }

    private static WhileStatement ParseWhileStmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.Advance();
        tokens.Expect(ValkyrieTokenKind.ParenthesisL);
        var cond = tokens.ParseExpressionNode();
        tokens.Expect(ValkyrieTokenKind.ParenthesisR);
        var body = tokens.ParseBlockNode(language);
        return new WhileStatement { Condition = cond, Body = body };
    }

    private static LoopInStatement ParseLoopStmt(TokenStream tokens, ValkyrieLanguage language)
    {
        tokens.Advance();
        string? iterName = null;
        ValkyrieNode? iterable = null;

        if (!tokens.Check(ValkyrieTokenKind.BraceL))
        {
            if (tokens.Check(ValkyrieTokenKind.In))
            {
                tokens.Advance();
                iterable = tokens.ParseExpressionNode();
            }
            else
            {
                iterName = tokens.AdvanceText();
                if (tokens.Check(ValkyrieTokenKind.In))
                {
                    tokens.Advance();
                }

                iterable = tokens.ParseExpressionNode();
            }
        }

        var body = tokens.ParseBlockNode(language);
        return new LoopInStatement(iterName, iterable, body, default);
    }

    private static void ExpectOrWarnSemicolon(TokenStream tokens)
    {
        if (tokens.Match(ValkyrieTokenKind.Semicolon))
        {
            return;
        }

        tokens.Diagnostics?.AddWarning("", new TextSpan(tokens.Position, 1), "PARSE", "缺少分号");
    }
}
