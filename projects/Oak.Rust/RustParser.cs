using System.Text;
using Oak.Diagnostics;
using Oak.Parsing;
using Oak.Syntax;

namespace Oak.Rust;

/// <summary>
///     Rust 语言语法解析器
/// </summary>
public sealed class RustParser : ParserBase<IReadOnlyList<RustToken>, RustAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<RustToken> _tokens = [];

    /// <summary>
    ///     创建 Rust 语法解析器
    /// </summary>
    public RustParser(DiagnosticSink? diagnostics = null)
        : base(diagnostics)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     解析词法单元序列
    /// </summary>
    public override RustAstNode Parse(IReadOnlyList<RustToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var items = new List<RustAstNode>();

        while (!IsAtEnd())
        {
            SkipNewLines();

            if (IsAtEnd()) break;

            var item = ParseItem();
            if (item is not null) items.Add(item);
        }

        return new RustCrate(items);
    }

    #region Types

    private RustAstNode ParseType()
    {
        var isReference = false;
        var isMutable = false;

        if (Match(RustNodeKind.Operator, "&"))
        {
            isReference = true;
            isMutable = Match(RustNodeKind.Keyword, "mut");
        }

        if (Check(RustNodeKind.Delimiter, "("))
        {
            Advance();
            var elements = new List<RustAstNode>();

            if (!Check(RustNodeKind.Delimiter, ")"))
            {
                do
                {
                    elements.Add(ParseType());
                } while (Match(RustNodeKind.Delimiter, ","));
            }

            Consume(RustNodeKind.Delimiter, "OR2051", "期望 ')'");
            return new RustTypeNode("tuple", isReference, isMutable);
        }

        if (Check(RustNodeKind.Delimiter, "["))
        {
            Advance();
            var elementType = ParseType();

            if (Match(RustNodeKind.Operator, ";")) ParseExpression();

            Consume(RustNodeKind.Delimiter, "OR2052", "期望 ']'");
            return new RustTypeNode("array", isReference, isMutable);
        }

        var typeName = Consume(RustNodeKind.Identifier, "OR2053", "期望类型名").Text;

        if (Check(RustNodeKind.Operator, "<"))
        {
            Advance();
            do
            {
                ParseType();
            } while (Match(RustNodeKind.Delimiter, ","));

            Consume(RustNodeKind.Operator, "OR2054", "期望 '>'");
        }

        return new RustTypeNode(typeName, isReference, isMutable);
    }

    #endregion

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Kind == RustNodeKind.Operator && Previous().Text == ";") return;

            if (Check(RustNodeKind.Keyword))
            {
                var value = Peek().Text;
                if (value is "fn" or "struct" or "enum" or "impl" or "trait" or "let" or "if" or "while" or "for"
                    or "return" or "use" or "mod") return;
            }

            Advance();
        }
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().Kind == RustNodeKind.Eof;
    }

    private RustToken Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private RustToken Previous()
    {
        return _tokens[_current - 1];
    }

    private RustToken Advance()
    {
        if (!IsAtEnd()) _current++;

        return Previous();
    }

    private bool Check(NodeKind kind)
    {
        return !IsAtEnd() && Peek().Kind == kind;
    }

    private bool Check(NodeKind kind, string value)
    {
        return !IsAtEnd() && Peek().Kind == kind && Peek().Text == value;
    }

    private bool Match(NodeKind kind)
    {
        if (Check(kind))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(NodeKind kind, string value)
    {
        if (Check(kind, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private RustToken Consume(NodeKind kind, string errorCode, string message)
    {
        if (Check(kind)) return Advance();

        var token = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private void SkipNewLines()
    {
        while (Match(RustNodeKind.NewLine))
        {
        }
    }

    private RustToken PeekNext()
    {
        return _current + 1 < _tokens.Count ? _tokens[_current + 1] : _tokens[^1];
    }

    #endregion

    #region Items

    private RustAstNode? ParseItem()
    {
        try
        {
            SkipNewLines();

            if (IsAtEnd()) return null;

            if (Check(RustNodeKind.Keyword, "fn")) return ParseFunctionDef();

            if (Check(RustNodeKind.Keyword, "struct")) return ParseStructDef();

            if (Check(RustNodeKind.Keyword, "enum")) return ParseEnumDef();

            if (Check(RustNodeKind.Keyword, "impl")) return ParseImplDef();

            if (Check(RustNodeKind.Keyword, "trait")) return ParseTraitDef();

            if (Check(RustNodeKind.Keyword, "type")) return ParseTypeAlias();

            if (Check(RustNodeKind.Keyword, "use")) return ParseUseDecl();

            if (Check(RustNodeKind.Keyword, "mod")) return ParseModDecl();

            if (Check(RustNodeKind.Keyword, "pub"))
            {
                Advance();
                var inner = ParseItem();
                return inner;
            }

            return ParseStatement();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private RustAstNode ParseFunctionDef()
    {
        var startToken = Advance();
        var name = Consume(RustNodeKind.Identifier, "OR2001", "期望函数名").Text;
        Consume(RustNodeKind.Delimiter, "OR2002", "期望 '('");
        var parameters = new List<RustParam>();

        if (!Check(RustNodeKind.Delimiter, ")"))
        {
            do
            {
                SkipNewLines();
                if (Check(RustNodeKind.Delimiter, ")")) break;

                var paramName = Consume(RustNodeKind.Identifier, "OR2003", "期望参数名").Text;
                RustAstNode? paramType = null;

                if (Match(RustNodeKind.Operator, ":")) paramType = ParseType();

                parameters.Add(new RustParam(paramName, paramType));
            } while (Match(RustNodeKind.Delimiter, ","));
        }

        Consume(RustNodeKind.Delimiter, "OR2004", "期望 ')'");

        RustAstNode? returnType = null;
        if (Match(RustNodeKind.Operator, "->")) returnType = ParseType();

        var body = ParseBlockExpr();

        return new RustFunctionDef(name, parameters, returnType, body, default);
    }

    private RustAstNode ParseStructDef()
    {
        var startToken = Advance();
        var name = Consume(RustNodeKind.Identifier, "OR2005", "期望结构体名").Text;
        var fields = new List<RustFieldDef>();

        if (Match(RustNodeKind.Delimiter, "{"))
        {
            while (!Check(RustNodeKind.Delimiter, "}") && !IsAtEnd())
            {
                SkipNewLines();
                if (Check(RustNodeKind.Delimiter, "}")) break;

                var isPublic = Match(RustNodeKind.Keyword, "pub");
                var fieldName = Consume(RustNodeKind.Identifier, "OR2006", "期望字段名").Text;
                Consume(RustNodeKind.Operator, "OR2007", "期望 ':'");
                var fieldType = ParseType();

                fields.Add(new RustFieldDef(fieldName, fieldType, isPublic));

                if (!Check(RustNodeKind.Delimiter, "}")) Consume(RustNodeKind.Delimiter, "OR2008", "期望 ','");
            }

            Consume(RustNodeKind.Delimiter, "OR2009", "期望 '}'");
        }
        else if (Match(RustNodeKind.Delimiter, "("))
        {
            while (!Check(RustNodeKind.Delimiter, ")") && !IsAtEnd())
            {
                var fieldType = ParseType();
                fields.Add(new RustFieldDef($"_{fields.Count}", fieldType, false));

                if (!Check(RustNodeKind.Delimiter, ")")) Consume(RustNodeKind.Delimiter, "OR2010", "期望 ','");
            }

            Consume(RustNodeKind.Delimiter, "OR2011", "期望 ')'");
        }

        return new RustStructDef(name, fields, default);
    }

    private RustAstNode ParseEnumDef()
    {
        var startToken = Advance();
        var name = Consume(RustNodeKind.Identifier, "OR2012", "期望枚举名").Text;
        var variants = new List<RustVariantDef>();

        Consume(RustNodeKind.Delimiter, "OR2013", "期望 '{'");

        while (!Check(RustNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(RustNodeKind.Delimiter, "}")) break;

            var variantName = Consume(RustNodeKind.Identifier, "OR2014", "期望变体名").Text;
            List<RustAstNode>? variantFields = null;

            if (Check(RustNodeKind.Delimiter, "("))
            {
                Advance();
                variantFields = [];

                while (!Check(RustNodeKind.Delimiter, ")") && !IsAtEnd())
                {
                    variantFields.Add(ParseType());
                    if (!Check(RustNodeKind.Delimiter, ")")) Consume(RustNodeKind.Delimiter, "OR2015", "期望 ','");
                }

                Consume(RustNodeKind.Delimiter, "OR2016", "期望 ')'");
            }
            else if (Check(RustNodeKind.Delimiter, "{"))
            {
                Advance();
                variantFields = [];

                while (!Check(RustNodeKind.Delimiter, "}") && !IsAtEnd())
                {
                    var fieldName = Consume(RustNodeKind.Identifier, "OR2017", "期望字段名").Text;
                    Consume(RustNodeKind.Operator, "OR2018", "期望 ':'");
                    variantFields.Add(ParseType());
                    if (!Check(RustNodeKind.Delimiter, "}")) Consume(RustNodeKind.Delimiter, "OR2019", "期望 ','");
                }

                Consume(RustNodeKind.Delimiter, "OR2020", "期望 '}'");
            }

            variants.Add(new RustVariantDef(variantName, variantFields));

            if (!Check(RustNodeKind.Delimiter, "}")) Consume(RustNodeKind.Delimiter, "OR2021", "期望 ','");
        }

        Consume(RustNodeKind.Delimiter, "OR2022", "期望 '}'");
        return new RustEnumDef(name, variants, default);
    }

    private RustAstNode ParseImplDef()
    {
        var startToken = Advance();
        RustAstNode? trait = null;

        var firstType = ParseType();

        if (Match(RustNodeKind.Keyword, "for")) trait = firstType;

        var targetType = trait is not null ? ParseType() : firstType;
        Consume(RustNodeKind.Delimiter, "OR2023", "期望 '{'");

        var members = new List<RustAstNode>();
        while (!Check(RustNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(RustNodeKind.Delimiter, "}")) break;

            var member = ParseItem();
            if (member is not null) members.Add(member);
        }

        Consume(RustNodeKind.Delimiter, "OR2024", "期望 '}'");
        return new RustImplDef(trait, targetType, members, default);
    }

    private RustAstNode ParseTraitDef()
    {
        var startToken = Advance();
        var name = Consume(RustNodeKind.Identifier, "OR2025", "期望 trait 名").Text;
        var members = new List<RustAstNode>();

        Consume(RustNodeKind.Delimiter, "OR2026", "期望 '{'");

        while (!Check(RustNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(RustNodeKind.Delimiter, "}")) break;

            var member = ParseItem();
            if (member is not null) members.Add(member);
        }

        Consume(RustNodeKind.Delimiter, "OR2027", "期望 '}'");
        return new RustTraitDef(name, members, default);
    }

    private RustAstNode ParseTypeAlias()
    {
        var startToken = Advance();
        var name = Consume(RustNodeKind.Identifier, "OR2028", "期望类型别名").Text;
        Consume(RustNodeKind.Operator, "OR2029", "期望 '='");
        var type = ParseType();
        Consume(RustNodeKind.Operator, "OR2030", "期望 ';'");

        return new RustTypeAlias(name, type, default);
    }

    private RustAstNode ParseUseDecl()
    {
        var startToken = Advance();
        var sb = new StringBuilder();

        while (!Check(RustNodeKind.Operator, ";") && !IsAtEnd()) sb.Append(Advance().Text);

        Consume(RustNodeKind.Operator, "OR2031", "期望 ';'");

        string? alias = null;
        var path = sb.ToString();

        var asIndex = path.IndexOf(" as ", StringComparison.Ordinal);
        if (asIndex >= 0)
        {
            alias = path[(asIndex + 4)..].Trim();
            path = path[..asIndex].Trim();
        }

        return new RustUseDecl(path, alias, default);
    }

    private RustAstNode ParseModDecl()
    {
        var startToken = Advance();
        var name = Consume(RustNodeKind.Identifier, "OR2032", "期望模块名").Text;
        RustAstNode? body = null;

        if (Check(RustNodeKind.Delimiter, "{"))
        {
            body = ParseBlockExpr();
        }
        else
        {
            Consume(RustNodeKind.Operator, "OR2033", "期望 ';'");
        }

        return new RustModDecl(name, body, default);
    }

    #endregion

    #region Statements

    private RustAstNode ParseStatement()
    {
        SkipNewLines();

        if (Check(RustNodeKind.Keyword, "let")) return ParseLetStmt();

        if (Check(RustNodeKind.Keyword, "return")) return ParseReturnStmt();

        if (Check(RustNodeKind.Keyword, "break")) return ParseBreakStmt();

        if (Check(RustNodeKind.Keyword, "continue")) return ParseContinueStmt();

        if (Check(RustNodeKind.Keyword, "while")) return ParseWhileStmt();

        if (Check(RustNodeKind.Keyword, "loop")) return ParseLoopStmt();

        if (Check(RustNodeKind.Keyword, "for")) return ParseForStmt();

        if (Check(RustNodeKind.Keyword, "if")) return ParseIfExpr();

        if (Check(RustNodeKind.Keyword, "match")) return ParseMatchExpr();

        var expr = ParseExpression();

        if (Match(RustNodeKind.Operator, ";")) return new RustExprStmt(expr);

        return expr;
    }

    private RustAstNode ParseLetStmt()
    {
        var startToken = Advance();
        var isMutable = Match(RustNodeKind.Keyword, "mut");
        var pattern = ParsePattern();
        RustAstNode? type = null;

        if (Match(RustNodeKind.Operator, ":")) type = ParseType();

        RustAstNode? initializer = null;
        if (Match(RustNodeKind.Operator, "=")) initializer = ParseExpression();

        Consume(RustNodeKind.Operator, "OR2034", "期望 ';'");
        return new RustLetStmt(pattern, type, initializer, isMutable, default);
    }

    private RustAstNode ParsePattern()
    {
        if (Check(RustNodeKind.Identifier))
        {
            var token = Advance();
            return new RustIdentifier(token.Text, default);
        }

        if (Match(RustNodeKind.Delimiter, "("))
        {
            var elements = new List<RustAstNode>();

            while (!Check(RustNodeKind.Delimiter, ")") && !IsAtEnd())
            {
                elements.Add(ParsePattern());
                if (!Check(RustNodeKind.Delimiter, ")")) Consume(RustNodeKind.Delimiter, "OR2035", "期望 ','");
            }

            Consume(RustNodeKind.Delimiter, "OR2036", "期望 ')'");
            return new RustTupleExpr(elements);
        }

        var errorToken = Peek();
        return new RustIdentifier("_");
    }

    private RustAstNode ParseReturnStmt()
    {
        var startToken = Advance();
        RustAstNode? value = null;

        if (!Check(RustNodeKind.Operator, ";") && !Check(RustNodeKind.Delimiter, "}")) value = ParseExpression();

        Match(RustNodeKind.Operator, ";");
        return new RustReturnStmt(value, default);
    }

    private RustAstNode ParseBreakStmt()
    {
        var startToken = Advance();
        RustAstNode? value = null;

        if (!Check(RustNodeKind.Operator, ";") && !Check(RustNodeKind.Delimiter, "}")) value = ParseExpression();

        Match(RustNodeKind.Operator, ";");
        return new RustBreakStmt(value, default);
    }

    private RustAstNode ParseContinueStmt()
    {
        var startToken = Advance();
        Match(RustNodeKind.Operator, ";");
        return new RustContinueStmt();
    }

    private RustAstNode ParseWhileStmt()
    {
        var startToken = Advance();
        var condition = ParseExpression();
        var body = ParseBlockExpr();

        return new RustWhileStmt(condition, body, default);
    }

    private RustAstNode ParseLoopStmt()
    {
        var startToken = Advance();
        var body = ParseBlockExpr();

        return new RustLoopStmt(body, default);
    }

    private RustAstNode ParseForStmt()
    {
        var startToken = Advance();
        var pattern = ParsePattern();
        Consume(RustNodeKind.Keyword, "OR2037", "期望 'in'");
        var iterator = ParseExpression();
        var body = ParseBlockExpr();

        return new RustForStmt(pattern, iterator, body, default);
    }

    #endregion

    #region Expressions

    private RustAstNode ParseExpression()
    {
        return ParseAssignment();
    }

    private RustAstNode ParseAssignment()
    {
        var left = ParseRange();

        if (Check(RustNodeKind.Operator) &&
            Peek().Text is "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "&=" or "|=" or "^=" or "<<=" or ">>=")
        {
            var op = Advance().Text;
            var right = ParseAssignment();
            return new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode ParseRange()
    {
        var left = ParseOr();

        if (Check(RustNodeKind.Operator, ".."))
        {
            Advance();
            var right = ParseOr();
            return new RustRange(left, right, false);
        }

        if (Check(RustNodeKind.Operator, "..="))
        {
            Advance();
            var right = ParseOr();
            return new RustRange(left, right, true);
        }

        return left;
    }

    private RustAstNode ParseOr()
    {
        var left = ParseAnd();

        while (Match(RustNodeKind.Operator, "||"))
        {
            var op = Previous().Text;
            var right = ParseAnd();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode ParseAnd()
    {
        var left = ParseComparison();

        while (Match(RustNodeKind.Operator, "&&"))
        {
            var op = Previous().Text;
            var right = ParseComparison();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode ParseComparison()
    {
        var left = ParseBitOr();

        while (Check(RustNodeKind.Operator) && Peek().Text is "==" or "!=" or "<" or ">" or "<=" or ">=")
        {
            var op = Advance().Text;
            var right = ParseBitOr();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode ParseBitOr()
    {
        var left = ParseBitXor();

        while (Match(RustNodeKind.Operator, "|"))
        {
            var right = ParseBitXor();
            left = new RustBinaryOp(left, "|", right);
        }

        return left;
    }

    private RustAstNode ParseBitXor()
    {
        var left = ParseBitAnd();

        while (Match(RustNodeKind.Operator, "^"))
        {
            var right = ParseBitAnd();
            left = new RustBinaryOp(left, "^", right);
        }

        return left;
    }

    private RustAstNode ParseBitAnd()
    {
        var left = ParseShift();

        while (Match(RustNodeKind.Operator, "&"))
        {
            var right = ParseShift();
            left = new RustBinaryOp(left, "&", right);
        }

        return left;
    }

    private RustAstNode ParseShift()
    {
        var left = ParseAdditive();

        while (Check(RustNodeKind.Operator) && Peek().Text is "<<" or ">>")
        {
            var op = Advance().Text;
            var right = ParseAdditive();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Check(RustNodeKind.Operator) && Peek().Text is "+" or "-")
        {
            var op = Advance().Text;
            var right = ParseMultiplicative();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode ParseMultiplicative()
    {
        var left = ParseCast();

        while (Check(RustNodeKind.Operator) && Peek().Text is "*" or "/" or "%")
        {
            var op = Advance().Text;
            var right = ParseCast();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode ParseCast()
    {
        var left = ParseUnary();

        if (Check(RustNodeKind.Keyword, "as"))
        {
            Advance();
            var type = ParseType();
            return new RustCast(left, type);
        }

        return left;
    }

    private RustAstNode ParseUnary()
    {
        if (Check(RustNodeKind.Operator) && Peek().Text is "-" or "!" or "*" or "&")
        {
            var op = Advance().Text;
            var operand = ParseUnary();
            return new RustUnaryOp(op, operand);
        }

        return ParsePostfix();
    }

    private RustAstNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Match(RustNodeKind.Operator, "."))
            {
                if (Check(RustNodeKind.Identifier) && PeekNext().Kind == RustNodeKind.Delimiter &&
                    PeekNext().Text == "(")
                {
                    var method = Advance().Text;
                    Consume(RustNodeKind.Delimiter, "OR2038", "期望 '('");
                    var args = new List<RustAstNode>();

                    if (!Check(RustNodeKind.Delimiter, ")"))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        } while (Match(RustNodeKind.Delimiter, ","));
                    }

                    Consume(RustNodeKind.Delimiter, "OR2039", "期望 ')'");
                    expr = new RustMethodCall(expr, method, args);
                }
                else
                {
                    var field = Consume(RustNodeKind.Identifier, "OR2040", "期望字段名").Text;
                    expr = new RustFieldAccess(expr, field);
                }
            }
            else if (Match(RustNodeKind.Delimiter, "["))
            {
                var index = ParseExpression();
                Consume(RustNodeKind.Delimiter, "OR2041", "期望 ']'");
                expr = new RustIndex(expr, index);
            }
            else if (Match(RustNodeKind.Delimiter, "("))
            {
                var args = new List<RustAstNode>();

                if (!Check(RustNodeKind.Delimiter, ")"))
                {
                    do
                    {
                        args.Add(ParseExpression());
                    } while (Match(RustNodeKind.Delimiter, ","));
                }

                Consume(RustNodeKind.Delimiter, "OR2042", "期望 ')'");
                expr = new RustCall(expr, args);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private RustAstNode ParsePrimary()
    {
        if (Match(RustNodeKind.Number))
        {
            var token = Previous();
            return new RustLiteral("number", token.Text, default);
        }

        if (Match(RustNodeKind.String))
        {
            var token = Previous();
            return new RustLiteral("string", token.Text, default);
        }

        if (Match(RustNodeKind.Char))
        {
            var token = Previous();
            return new RustLiteral("char", token.Text, default);
        }

        if (Check(RustNodeKind.Keyword, "true"))
        {
            var token = Advance();
            return new RustLiteral("bool", "true", default);
        }

        if (Check(RustNodeKind.Keyword, "false"))
        {
            var token = Advance();
            return new RustLiteral("bool", "false", default);
        }

        if (Check(RustNodeKind.Identifier))
        {
            var token = Advance();
            return new RustIdentifier(token.Text, default);
        }

        if (Check(RustNodeKind.Delimiter, "("))
        {
            Advance();
            var expr = ParseExpression();
            Consume(RustNodeKind.Delimiter, "OR2043", "期望 ')'");
            return expr;
        }

        if (Check(RustNodeKind.Delimiter, "[")) return ParseArrayExpr();

        if (Check(RustNodeKind.Keyword, "if")) return ParseIfExpr();

        if (Check(RustNodeKind.Keyword, "match")) return ParseMatchExpr();

        if (Check(RustNodeKind.Delimiter, "{")) return ParseBlockExpr();

        if (Check(RustNodeKind.Identifier) && Peek().Text.StartsWith('!')) return ParseMacroCall();

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            "OR2044",
            $"意外的标记 '{errorToken.Text}'");

        throw new ParseException($"意外的标记 '{errorToken.Text}'");
    }

    private RustAstNode ParseIfExpr()
    {
        var startToken = Advance();
        var condition = ParseExpression();
        var thenBranch = ParseBlockExpr();
        RustAstNode? elseBranch = null;

        if (Match(RustNodeKind.Keyword, "else"))
        {
            if (Check(RustNodeKind.Keyword, "if"))
            {
                elseBranch = ParseIfExpr();
            }
            else
            {
                elseBranch = ParseBlockExpr();
            }
        }

        return new RustIfExpr(condition, thenBranch, elseBranch, default);
    }

    private RustAstNode ParseMatchExpr()
    {
        var startToken = Advance();
        var scrutinee = ParseExpression();
        Consume(RustNodeKind.Delimiter, "OR2045", "期望 '{'");

        var arms = new List<RustMatchArm>();
        while (!Check(RustNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(RustNodeKind.Delimiter, "}")) break;

            var pattern = ParsePattern();
            Match(RustNodeKind.Keyword, "if");
            Consume(RustNodeKind.Operator, "OR2046", "期望 '=>'");
            var body = ParseExpression();
            Match(RustNodeKind.Operator, ",");

            arms.Add(new RustMatchArm(pattern, body));
        }

        Consume(RustNodeKind.Delimiter, "OR2047", "期望 '}'");
        return new RustMatchExpr(scrutinee, arms, default);
    }

    private RustAstNode ParseArrayExpr()
    {
        var startToken = Advance();
        var elements = new List<RustAstNode>();

        if (!Check(RustNodeKind.Delimiter, "]"))
        {
            do
            {
                elements.Add(ParseExpression());
            } while (Match(RustNodeKind.Delimiter, ","));
        }

        Consume(RustNodeKind.Delimiter, "OR2048", "期望 ']'");
        return new RustArrayExpr(elements, default);
    }

    private RustAstNode ParseBlockExpr()
    {
        var startToken = Consume(RustNodeKind.Delimiter, "OR2049", "期望 '{'");
        var statements = new List<RustAstNode>();

        while (!Check(RustNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(RustNodeKind.Delimiter, "}")) break;

            var stmt = ParseStatement();
            statements.Add(stmt);
        }

        Consume(RustNodeKind.Delimiter, "OR2050", "期望 '}'");
        return new RustBlockExpr(statements, default);
    }

    private RustAstNode ParseMacroCall()
    {
        var startToken = Advance();
        var name = startToken.Text;
        var body = ParseExpression();

        return new RustMacroCall(name, body, default);
    }

    #endregion
}
