using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.AST.Pattern;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.Parser;

/// <summary>
/// 表达式解析扩展入口。
/// </summary>
internal static class ExpressionExtensions
{
    internal static ValkyrieNode ParseExpressionNode(this TokenStream tokens)
    {
        return ParseAssignment(tokens);
    }

    private static ValkyrieNode ParseAssignment(TokenStream tokens)
    {
        var left = PrattParser(tokens);
        if (IsAssignmentOperator(tokens))
        {
            var op = tokens.AdvanceText();
            var right = ParseAssignment(tokens);
            return new AssignmentExpr { Target = left, Operator = op, Value = right };
        }

        return left;
    }

    private static bool IsAssignmentOperator(TokenStream tokens)
    {
        if (!tokens.IsOperator())
        {
            return false;
        }

        var text = tokens.PeekText();
        return text is "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "&=" or "|=" or "^=" or "<<=" or ">>=";
    }

    private static ValkyrieNode PrattParser(TokenStream tokens, int minPrecedence = Precedence.Lowest)
    {
        var left = ParsePrefix(tokens);
        while (minPrecedence < CurrentPrecedence(tokens))
        {
            left = ParseInfix(tokens, left);
        }

        return left;
    }

    private static ValkyrieNode ParsePrefix(TokenStream tokens)
    {
        if (tokens.IsAtEnd())
        {
            tokens.Diagnostics?.AddWarning("", new TextSpan(tokens.Position, 1), "PARSE", "表达式意外结束");
            return new IdentifierNode { Name = "<eof>" };
        }

        if (tokens.Check(ValkyrieTokenKind.Number))
        {
            return new TermAtomicLiteral(LiteralType.Number, tokens.AdvanceText());
        }

        if (tokens.Check(ValkyrieTokenKind.String))
        {
            return new TermAtomicLiteral(LiteralType.String, tokens.AdvanceText());
        }

        if (tokens.IsLiteral())
        {
            var text = tokens.AdvanceText().ToLowerInvariant();
            var kind = text switch
            {
                "true" => LiteralType.Boolean,
                "false" => LiteralType.Boolean,
                "null" => LiteralType.Null,
                _ => LiteralType.String
            };
            return new TermAtomicLiteral(kind, text);
        }

        if (tokens.IsOperator())
        {
            var op = tokens.PeekText();
            if (IsPrefixOperator(op) || IsPrefixUpdateOperator(op))
            {
                tokens.Advance();
                var operand = PrattParser(tokens, Precedence.Prefix);
                return new TermUnaryExpression { Operator = op, Operand = operand, IsPrefix = true };
            }
        }

        if (tokens.Check(ValkyrieTokenKind.Identifier) || tokens.IsKeyword())
        {
            var name = tokens.AdvanceText();
            if (tokens.Check(ValkyrieTokenKind.ParenthesisL))
            {
                tokens.Advance();
                var args = ParseArgList(tokens);
                tokens.Expect(ValkyrieTokenKind.ParenthesisR);
                return new TermCallExpression
                {
                    Callee = new IdentifierNode { Name = name },
                    Arguments = args
                };
            }

            return new IdentifierNode { Name = name };
        }

        if (tokens.Check(ValkyrieTokenKind.ParenthesisL))
        {
            tokens.Advance();
            var expr = tokens.ParseExpressionNode();
            tokens.Expect(ValkyrieTokenKind.ParenthesisR);
            return expr;
        }

        if (tokens.Check(ValkyrieTokenKind.BracketL))
        {
            return ParseArrayLiteral(tokens);
        }

        var unexpectedText = tokens.AdvanceText();
        tokens.Diagnostics?.AddWarning("", new TextSpan(tokens.Position - 1, 1), "PARSE", $"无法识别的表达式元素：\"{unexpectedText}\"");
        return new IdentifierNode { Name = "<unknown>" };
    }

    private static ValkyrieNode ParseInfix(TokenStream tokens, ValkyrieNode left)
    {
        if (tokens.IsOperator())
        {
            var op = tokens.PeekText();
            if (IsPostfixOperator(op))
            {
                tokens.Advance();
                return new TermUnaryExpression { Operator = op, Operand = left, IsPrefix = false };
            }
        }

        if (tokens.Check(ValkyrieTokenKind.ParenthesisL))
        {
            return ParseCall(tokens, left);
        }

        if (tokens.Check(ValkyrieTokenKind.DoubleColon))
        {
            return ParseOffsetAliasIndex(tokens, left);
        }

        if (tokens.Check(ValkyrieTokenKind.BracketL))
        {
            return ParseOrdinalIndex(tokens, left);
        }

        if (tokens.Check(ValkyrieTokenKind.Dot))
        {
            return ParseMemberAccess(tokens, left);
        }

        if (tokens.Check(ValkyrieTokenKind.Identifier))
        {
            return ParseImplicitMemberAccess(tokens, left);
        }

        if (tokens.IsKeyword())
        {
            var keyword = tokens.PeekText();
            if (keyword is "is" or "as")
            {
                tokens.Advance();
                var span = new TextSpan(tokens.Position - 1, 1);
                if (keyword == "is")
                {
                    var targetPattern = ParseTypePatternForIs(tokens);
                    return new TermIsExpression(left, targetPattern, span);
                }

                var targetType = ParseTypeAnnotationForCast(tokens);
                if (tokens.Check(ValkyrieTokenKind.Question))
                {
                    tokens.Advance();
                    return new TermAsExpression(left, targetType, span);
                }

                return new TermAsExpression(left, targetType, span);
            }
        }

        if (tokens.IsOperator())
        {
            var op = tokens.PeekText();
            if (IsBinaryOperator(op))
            {
                var precedence = CurrentPrecedence(tokens);
                tokens.Advance();
                var right = IsRightAssociativeBinary(op) ? PrattParser(tokens, precedence - 1) : PrattParser(tokens, precedence);
                return new TermBinaryExpression { Left = left, Operator = op, Right = right };
            }
        }

        return left;
    }

    private static int CurrentPrecedence(TokenStream tokens)
    {
        if (tokens.IsAtEnd())
        {
            return Precedence.Lowest;
        }

        if (tokens.Check(ValkyrieTokenKind.ParenthesisL) ||
            tokens.Check(ValkyrieTokenKind.DoubleColon) ||
            tokens.Check(ValkyrieTokenKind.BracketL) ||
            tokens.Check(ValkyrieTokenKind.Dot))
        {
            return Precedence.Call;
        }

        if (tokens.Check(ValkyrieTokenKind.Identifier))
        {
            var nextKind = tokens.PeekKind(1);
            if (nextKind == ValkyrieTokenKind.Colon)
            {
                return Precedence.Lowest;
            }

            return Precedence.Call;
        }

        if (!tokens.IsOperator())
        {
            return Precedence.Lowest;
        }

        var op = tokens.PeekText();
        if (IsPostfixOperator(op))
        {
            return Precedence.Postfix;
        }

        return GetBinaryPrecedence(op);
    }

    private static ValkyrieNode ParseCall(TokenStream tokens, ValkyrieNode callee)
    {
        tokens.Advance();
        var args = ParseArgList(tokens);
        tokens.Expect(ValkyrieTokenKind.ParenthesisR);
        return new TermCallExpression { Callee = callee, Arguments = args };
    }

    private static ValkyrieNode ParseOrdinalIndex(TokenStream tokens, ValkyrieNode target)
    {
        tokens.Advance();
        var indices = ParseIndexArguments(tokens, ValkyrieTokenKind.BracketR.ToNodeKind());
        return new TermOrdinalExpression { Target = target, Indices = indices };
    }

    private static ValkyrieNode ParseOffsetAliasIndex(TokenStream tokens, ValkyrieNode target)
    {
        tokens.Advance();
        tokens.Expect(ValkyrieTokenKind.BracketL);
        var indices = ParseIndexArguments(tokens, ValkyrieTokenKind.BracketR.ToNodeKind());
        return new TermOffsetExpression { Target = target, Indices = indices };
    }

    private static IReadOnlyList<ValkyrieNode> ParseIndexArguments(TokenStream tokens, NodeKind closeKind)
    {
        var indices = new List<ValkyrieNode>();
        if (tokens.Check(closeKind))
        {
            tokens.Advance();
            return indices;
        }

        while (true)
        {
            indices.Add(tokens.ParseExpressionNode());
            if (tokens.Check(ValkyrieTokenKind.Comma))
            {
                tokens.Advance();
                continue;
            }

            tokens.Expect(closeKind);
            break;
        }

        return indices;
    }

    private static ValkyrieNode ParseMemberAccess(TokenStream tokens, ValkyrieNode target)
    {
        tokens.Advance();
        var member = tokens.AdvanceText();
        return new TermDotExpression { Target = target, MemberName = member };
    }

    private static PatternNode ParseTypePatternForIs(TokenStream tokens)
    {
        var type = ParseTypeAnnotationForCast(tokens);
        return new PatternNode(type, null, default);
    }

    private static TypeNode ParseTypeAnnotationForCast(TokenStream tokens)
    {
        if (tokens.IsKeyword() || tokens.Check(ValkyrieTokenKind.Identifier) || tokens.IsLiteral())
        {
            var name = tokens.AdvanceText();
            var typeNode = new TypeNode(name, []);
            if (tokens.Check(ValkyrieTokenKind.Question))
            {
                tokens.Advance();
                return new TypeUnaryExpression("?", typeNode, false);
            }

            return typeNode;
        }

        var unexpectedText = tokens.AdvanceText();
        tokens.Diagnostics?.AddError("", new TextSpan(tokens.Position - 1, 1), "PARSE", $"期望类型注解，但遇到 \"{unexpectedText}\"");
        return new TypeNode("any", []);
    }

    private static bool IsPrefixOperator(string op) => op is "-" or "!" or "~";

    private static bool IsPrefixUpdateOperator(string op) => op is "++" or "--";

    private static bool IsPostfixOperator(string op) => op is "++" or "--";

    private static bool IsBinaryOperator(string op)
    {
        return op is "==" or "!=" or "<" or ">" or "<=" or ">="
            or "&&" or "||"
            or "+" or "-" or "*" or "/" or "%"
            or "&" or "|" or "^" or "<<" or ">>"
            or "?";
    }

    private static bool IsRightAssociativeBinary(string op) => op is "=>";

    private static int GetBinaryPrecedence(string op)
    {
        return op switch
        {
            "==" or "!=" => Precedence.Equality,
            "<" or ">" or "<=" or ">=" => Precedence.Comparison,
            "<<" or ">>" => Precedence.Shift,
            "+" or "-" => Precedence.Sum,
            "*" or "/" or "%" => Precedence.Product,
            "&&" => Precedence.LogicalAnd,
            "||" => Precedence.LogicalOr,
            "&" or "|" or "^" => Precedence.Bitwise,
            _ => Precedence.Lowest
        };
    }

    private static IReadOnlyList<ValkyrieNode> ParseArgList(TokenStream tokens)
    {
        var args = new List<ValkyrieNode>();
        if (tokens.Check(ValkyrieTokenKind.ParenthesisR))
        {
            return args;
        }

        while (!tokens.IsAtEnd() && !tokens.Check(ValkyrieTokenKind.ParenthesisR))
        {
            args.Add(tokens.ParseExpressionNode());
            if (tokens.Check(ValkyrieTokenKind.Comma))
            {
                tokens.Advance();
                continue;
            }

            break;
        }

        return args;
    }

    private static ValkyrieNode ParseArrayLiteral(TokenStream tokens)
    {
        tokens.Expect(ValkyrieTokenKind.BracketL);
        var elements = new List<ValkyrieNode>();
        if (tokens.Check(ValkyrieTokenKind.BracketR))
        {
            tokens.Advance();
            return new TermArrayLiteral { Elements = [] };
        }

        while (!tokens.IsAtEnd() && !tokens.Check(ValkyrieTokenKind.BracketR))
        {
            elements.Add(tokens.ParseExpressionNode());
            if (tokens.Check(ValkyrieTokenKind.Comma))
            {
                tokens.Advance();
                continue;
            }

            break;
        }

        tokens.Expect(ValkyrieTokenKind.BracketR);
        return new TermArrayLiteral { Elements = elements };
    }

    private static ValkyrieNode ParseImplicitMemberAccess(TokenStream tokens, ValkyrieNode target)
    {
        var member = tokens.AdvanceText();
        return new TermDotExpression { Target = target, MemberName = member };
    }
}
