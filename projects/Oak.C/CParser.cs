using Oak.Diagnostics;
using Oak.Parsing;
using Oak.Syntax;

namespace Oak.C;

/// <summary>
///     C 语言语法解析器
/// </summary>
public sealed class CParser : ParserBase<IReadOnlyList<CToken>, CAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<CToken> _tokens = [];

    public CParser(DiagnosticSink? diagnostics = null)
        : base(diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public override CAstNode Parse(IReadOnlyList<CToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var declarations = new List<CAstNode>();

        while (!IsAtEnd())
        {
            SkipNewLines();

            if (IsAtEnd()) break;

            var decl = ParseExternalDeclaration();
            if (decl is not null) declarations.Add(decl);
        }

        return new CTranslationUnit(declarations);
    }

    #region Type Specifier

    private CAstNode ParseTypeSpecifier()
    {
        var startToken = Peek();
        var isConst = false;
        var isPointer = false;

        if (Match(CNodeKind.Keyword, "const")) isConst = true;

        string typeName;

        if (Check(CNodeKind.Keyword, "struct") || Check(CNodeKind.Keyword, "union") ||
            Check(CNodeKind.Keyword, "enum"))
        {
            var kw = Advance().Text;
            if (Check(CNodeKind.Identifier))
                typeName = $"{kw} {Advance().Text}";
            else
                typeName = kw;
        }
        else if (Check(CNodeKind.Keyword) || Check(CNodeKind.Identifier))
        {
            typeName = Advance().Text;
        }
        else
        {
            throw new ParseException("期望类型说明符");
        }

        while (Match(CNodeKind.Operator, "*")) isPointer = true;

        if (Match(CNodeKind.Keyword, "const")) isConst = true;

        return new CTypeNode(typeName, isPointer, isConst, default);
    }

    #endregion

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Kind == CNodeKind.Delimiter && Previous().Text == ";") return;

            if (Check(CNodeKind.Keyword))
            {
                var value = Peek().Text;
                if (value is "int" or "char" or "float" or "double" or "void" or "struct" or "if" or "while" or "for"
                    or "return" or "typedef") return;
            }

            Advance();
        }
    }

    private CToken PeekNext()
    {
        return _current + 1 < _tokens.Count ? _tokens[_current + 1] : _tokens[^1];
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().Kind == CNodeKind.Eof;
    }

    private CToken Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private CToken Previous()
    {
        return _tokens[_current - 1];
    }

    private CToken Advance()
    {
        if (!IsAtEnd()) _current++;

        return Previous();
    }

    private bool Check(NodeKind type)
    {
        return !IsAtEnd() && Peek().Kind == type;
    }

    private bool Check(NodeKind type, string value)
    {
        return !IsAtEnd() && Peek().Kind == type && Peek().Text == value;
    }

    private bool Match(NodeKind type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(NodeKind type, string value)
    {
        if (Check(type, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private CToken Consume(NodeKind type, string errorCode, string message)
    {
        if (Check(type)) return Advance();

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
        while (Match(CNodeKind.NewLine))
        {
        }
    }

    #endregion

    #region External Declarations

    private CAstNode? ParseExternalDeclaration()
    {
        try
        {
            SkipNewLines();

            if (IsAtEnd()) return null;

            if (Check(CNodeKind.Keyword, "typedef")) return ParseTypedef();

            if (Check(CNodeKind.Keyword, "struct") || Check(CNodeKind.Keyword, "union")) return ParseStructOrUnion();

            if (Check(CNodeKind.Keyword, "enum")) return ParseEnum();

            return ParseDeclarationOrFunction();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private CAstNode ParseTypedef()
    {
        var startToken = Advance();
        var type = ParseTypeSpecifier();
        var name = Consume(CNodeKind.Identifier, "OC2001", "期望类型别名名称").Text;
        Consume(CNodeKind.Delimiter, "OC2002", "期望 ';'");

        return new CTypedef(type, name, default);
    }

    private CAstNode ParseStructOrUnion()
    {
        var startToken = Advance();
        string? name = null;

        if (Check(CNodeKind.Identifier)) name = Advance().Text;

        if (Match(CNodeKind.Delimiter, "{"))
        {
            var fields = new List<CVarDecl>();

            while (!Check(CNodeKind.Delimiter, "}") && !IsAtEnd())
            {
                SkipNewLines();
                if (Check(CNodeKind.Delimiter, "}")) break;

                var fieldType = ParseTypeSpecifier();
                var fieldName = Consume(CNodeKind.Identifier, "OC2003", "期望字段名").Text;
                CAstNode? init = null;

                if (Match(CNodeKind.Operator, "=")) init = ParseExpression();

                Consume(CNodeKind.Delimiter, "OC2004", "期望 ';'");
                fields.Add(new CVarDecl(fieldType, fieldName, init));
            }

            Consume(CNodeKind.Delimiter, "OC2005", "期望 '}'");

            if (name is not null) Consume(CNodeKind.Delimiter, "OC2006", "期望 ';'");

            return new CStructDef(name, fields, default);
        }

        if (name is not null)
        {
            Consume(CNodeKind.Delimiter, "OC2007", "期望 ';'");
            return new CStructDef(name, [], default);
        }

        throw new ParseException("结构体定义语法错误");
    }

    private CAstNode ParseEnum()
    {
        var startToken = Advance();
        string? name = null;

        if (Check(CNodeKind.Identifier)) name = Advance().Text;

        if (Match(CNodeKind.Delimiter, "{"))
        {
            while (!Check(CNodeKind.Delimiter, "}") && !IsAtEnd())
            {
                SkipNewLines();
                if (Check(CNodeKind.Delimiter, "}")) break;

                Consume(CNodeKind.Identifier, "OC2008", "期望枚举常量名");

                if (Match(CNodeKind.Operator, "=")) ParseExpression();

                if (!Check(CNodeKind.Delimiter, "}")) Consume(CNodeKind.Delimiter, "OC2009", "期望 ','");
            }

            Consume(CNodeKind.Delimiter, "OC2010", "期望 '}'");
        }

        Consume(CNodeKind.Delimiter, "OC2011", "期望 ';'");
        return new CStructDef(name, [], default);
    }

    private CAstNode ParseDeclarationOrFunction()
    {
        var startToken = Peek();
        var type = ParseTypeSpecifier();
        var name = Consume(CNodeKind.Identifier, "OC2012", "期望标识符").Text;

        if (Check(CNodeKind.Delimiter, "(")) return ParseFunctionDefinition(type, name, default);

        CAstNode? init = null;

        if (Match(CNodeKind.Operator, "=")) init = ParseInitializer();

        Consume(CNodeKind.Delimiter, "OC2013", "期望 ';'");
        return new CVarDecl(type, name, init, default);
    }

    private CAstNode ParseFunctionDefinition(CAstNode returnType, string name, CToken startToken)
    {
        Consume(CNodeKind.Delimiter, "OC2014", "期望 '('");
        var parameters = new List<CParamDecl>();

        if (!Check(CNodeKind.Delimiter, ")"))
            do
            {
                SkipNewLines();
                if (Check(CNodeKind.Delimiter, ")")) break;

                if (Check(CNodeKind.Keyword, "void"))
                {
                    Advance();
                    if (Check(CNodeKind.Delimiter, ")")) break;
                }

                var paramType = ParseTypeSpecifier();
                var paramName = Consume(CNodeKind.Identifier, "OC2015", "期望参数名").Text;
                parameters.Add(new CParamDecl(paramType, paramName));
            } while (Match(CNodeKind.Delimiter, ","));

        Consume(CNodeKind.Delimiter, "OC2016", "期望 ')'");
        var body = ParseCompoundStatement();

        return new CFunctionDef(returnType, name, parameters, body, default);
    }

    #endregion

    #region Statements

    private CAstNode ParseStatement()
    {
        SkipNewLines();

        if (Check(CNodeKind.Delimiter, "{")) return ParseCompoundStatement();

        if (Check(CNodeKind.Keyword, "if")) return ParseIfStmt();

        if (Check(CNodeKind.Keyword, "while")) return ParseWhileStmt();

        if (Check(CNodeKind.Keyword, "do")) return ParseDoWhileStmt();

        if (Check(CNodeKind.Keyword, "for")) return ParseForStmt();

        if (Check(CNodeKind.Keyword, "return")) return ParseReturnStmt();

        if (Check(CNodeKind.Keyword, "break")) return ParseBreakStmt();

        if (Check(CNodeKind.Keyword, "continue")) return ParseContinueStmt();

        if (Check(CNodeKind.Keyword, "switch")) return ParseSwitchStmt();

        if (Check(CNodeKind.Keyword, "goto")) return ParseGotoStmt();

        if (Check(CNodeKind.Identifier) && PeekNext().Kind == CNodeKind.Delimiter && PeekNext().Text == ":")
            return ParseLabelStmt();

        return ParseExprOrDeclStmt();
    }

    private CAstNode ParseCompoundStatement()
    {
        var startToken = Consume(CNodeKind.Delimiter, "OC2017", "期望 '{'");
        var statements = new List<CAstNode>();

        while (!Check(CNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(CNodeKind.Delimiter, "}")) break;

            var stmt = ParseStatement();
            statements.Add(stmt);
        }

        Consume(CNodeKind.Delimiter, "OC2018", "期望 '}'");
        return new CCompound(statements, default);
    }

    private CAstNode ParseIfStmt()
    {
        var startToken = Advance();
        Consume(CNodeKind.Delimiter, "OC2019", "期望 '('");
        var condition = ParseExpression();
        Consume(CNodeKind.Delimiter, "OC2020", "期望 ')'");
        var thenBody = ParseStatement();
        CAstNode? elseBody = null;

        if (Match(CNodeKind.Keyword, "else")) elseBody = ParseStatement();

        return new CIf(condition, thenBody, elseBody, default);
    }

    private CAstNode ParseWhileStmt()
    {
        var startToken = Advance();
        Consume(CNodeKind.Delimiter, "OC2021", "期望 '('");
        var condition = ParseExpression();
        Consume(CNodeKind.Delimiter, "OC2022", "期望 ')'");
        var body = ParseStatement();

        return new CWhile(condition, body, default);
    }

    private CAstNode ParseDoWhileStmt()
    {
        var startToken = Advance();
        var body = ParseStatement();
        Consume(CNodeKind.Keyword, "OC2023", "期望 'while'");
        Consume(CNodeKind.Delimiter, "OC2024", "期望 '('");
        var condition = ParseExpression();
        Consume(CNodeKind.Delimiter, "OC2025", "期望 ')'");
        Consume(CNodeKind.Delimiter, "OC2026", "期望 ';'");

        return new CDoWhile(body, condition, default);
    }

    private CAstNode ParseForStmt()
    {
        var startToken = Advance();
        Consume(CNodeKind.Delimiter, "OC2027", "期望 '('");
        CAstNode? init = null;

        if (!Check(CNodeKind.Delimiter, ";"))
            init = ParseExprOrDeclStmt();
        else
            Consume(CNodeKind.Delimiter, "OC2028", "期望 ';'");

        CAstNode? condition = null;
        if (!Check(CNodeKind.Delimiter, ";")) condition = ParseExpression();
        Consume(CNodeKind.Delimiter, "OC2029", "期望 ';'");

        CAstNode? increment = null;
        if (!Check(CNodeKind.Delimiter, ")")) increment = ParseExpression();
        Consume(CNodeKind.Delimiter, "OC2030", "期望 ')'");

        var body = ParseStatement();
        return new CFor(init, condition, increment, body, default);
    }

    private CAstNode ParseReturnStmt()
    {
        var startToken = Advance();
        CAstNode? value = null;

        if (!Check(CNodeKind.Delimiter, ";")) value = ParseExpression();

        Consume(CNodeKind.Delimiter, "OC2031", "期望 ';'");
        return new CReturn(value, default);
    }

    private CAstNode ParseBreakStmt()
    {
        var startToken = Advance();
        Consume(CNodeKind.Delimiter, "OC2032", "期望 ';'");
        return new CBreak();
    }

    private CAstNode ParseContinueStmt()
    {
        var startToken = Advance();
        Consume(CNodeKind.Delimiter, "OC2033", "期望 ';'");
        return new CContinue();
    }

    private CAstNode ParseGotoStmt()
    {
        var startToken = Advance();
        var label = Consume(CNodeKind.Identifier, "OC2034", "期望标签名").Text;
        Consume(CNodeKind.Delimiter, "OC2035", "期望 ';'");
        return new CGoto(label, default);
    }

    private CAstNode ParseLabelStmt()
    {
        var name = Advance().Text;
        Consume(CNodeKind.Delimiter, "OC2036", "期望 ':'");
        var stmt = ParseStatement();
        return new CLabel(name, stmt);
    }

    private CAstNode ParseSwitchStmt()
    {
        var startToken = Advance();
        Consume(CNodeKind.Delimiter, "OC2037", "期望 '('");
        var expr = ParseExpression();
        Consume(CNodeKind.Delimiter, "OC2038", "期望 ')'");
        Consume(CNodeKind.Delimiter, "OC2039", "期望 '{'");

        var cases = new List<CAstNode>();

        while (!Check(CNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(CNodeKind.Delimiter, "}")) break;

            if (Match(CNodeKind.Keyword, "case"))
            {
                var value = ParseExpression();
                Consume(CNodeKind.Delimiter, "OC2040", "期望 ':'");
                var body = new List<CAstNode>();

                while (!Check(CNodeKind.Delimiter, "}") && !Check(CNodeKind.Keyword, "case") &&
                       !Check(CNodeKind.Keyword, "default") && !IsAtEnd()) body.Add(ParseStatement());

                cases.Add(new CCase(value, body));
            }
            else if (Match(CNodeKind.Keyword, "default"))
            {
                Consume(CNodeKind.Delimiter, "OC2041", "期望 ':'");
                var body = new List<CAstNode>();

                while (!Check(CNodeKind.Delimiter, "}") && !Check(CNodeKind.Keyword, "case") &&
                       !Check(CNodeKind.Keyword, "default") && !IsAtEnd()) body.Add(ParseStatement());

                cases.Add(new CCase(null, body));
            }
            else
            {
                throw new ParseException("Switch 语句中期望 case 或 default");
            }
        }

        Consume(CNodeKind.Delimiter, "OC2042", "期望 '}'");
        return new CSwitch(expr, cases, default);
    }

    private CAstNode ParseExprOrDeclStmt()
    {
        var startToken = Peek();

        if (IsTypeSpecifier(Peek()))
        {
            var type = ParseTypeSpecifier();
            var name = Consume(CNodeKind.Identifier, "OC2043", "期望变量名").Text;
            CAstNode? init = null;

            if (Match(CNodeKind.Operator, "=")) init = ParseInitializer();

            Consume(CNodeKind.Delimiter, "OC2044", "期望 ';'");
            return new CVarDecl(type, name, init, default);
        }

        var expr = ParseExpression();
        Consume(CNodeKind.Delimiter, "OC2045", "期望 ';'");
        return new CExprStmt(expr, default);
    }

    private bool IsTypeSpecifier(CToken token)
    {
        if (token.Kind != CNodeKind.Keyword) return false;

        return token.Text is "int" or "char" or "float" or "double" or "void" or "short" or "long"
            or "signed" or "unsigned" or "const" or "struct" or "union" or "enum" or "static"
            or "extern" or "auto" or "register" or "volatile" or "inline" or "restrict"
            or "_Bool" or "_Complex" or "_Imaginary" or "typedef";
    }

    private CAstNode ParseInitializer()
    {
        if (Check(CNodeKind.Delimiter, "{")) return ParseInitList();

        return ParseExpression();
    }

    private CAstNode ParseInitList()
    {
        var startToken = Consume(CNodeKind.Delimiter, "OC2046", "期望 '{'");
        var elements = new List<CAstNode>();

        if (!Check(CNodeKind.Delimiter, "}"))
            do
            {
                SkipNewLines();
                if (Check(CNodeKind.Delimiter, "}")) break;

                elements.Add(ParseInitializer());
            } while (Match(CNodeKind.Delimiter, ","));

        Consume(CNodeKind.Delimiter, "OC2047", "期望 '}'");
        return new CInitList(elements, default);
    }

    #endregion

    #region Expressions

    private CAstNode ParseExpression()
    {
        return ParseAssignment();
    }

    private CAstNode ParseAssignment()
    {
        var left = ParseConditional();

        if (Check(CNodeKind.Operator) &&
            Peek().Text is "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "&=" or "|=" or "^=" or "<<=" or ">>=")
        {
            var op = Advance().Text;
            var right = ParseAssignment();
            return new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseConditional()
    {
        var condition = ParseOr();

        if (Match(CNodeKind.Operator, "?"))
        {
            var thenExpr = ParseExpression();
            Consume(CNodeKind.Delimiter, "OC2048", "期望 ':'");
            var elseExpr = ParseConditional();
            return new CTernaryOp(condition, thenExpr, elseExpr);
        }

        return condition;
    }

    private CAstNode ParseOr()
    {
        var left = ParseAnd();

        while (Match(CNodeKind.Operator, "||"))
        {
            var op = Previous().Text;
            var right = ParseAnd();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseAnd()
    {
        var left = ParseBitOr();

        while (Match(CNodeKind.Operator, "&&"))
        {
            var op = Previous().Text;
            var right = ParseBitOr();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseBitOr()
    {
        var left = ParseBitXor();

        while (Match(CNodeKind.Operator, "|"))
        {
            var op = Previous().Text;
            var right = ParseBitXor();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseBitXor()
    {
        var left = ParseBitAnd();

        while (Match(CNodeKind.Operator, "^"))
        {
            var op = Previous().Text;
            var right = ParseBitAnd();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseBitAnd()
    {
        var left = ParseEquality();

        while (Match(CNodeKind.Operator, "&"))
        {
            var op = Previous().Text;
            var right = ParseEquality();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseEquality()
    {
        var left = ParseRelational();

        while (Check(CNodeKind.Operator) && Peek().Text is "==" or "!=")
        {
            var op = Advance().Text;
            var right = ParseRelational();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseRelational()
    {
        var left = ParseShift();

        while (Check(CNodeKind.Operator) && Peek().Text is "<" or ">" or "<=" or ">=")
        {
            var op = Advance().Text;
            var right = ParseShift();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseShift()
    {
        var left = ParseAdditive();

        while (Check(CNodeKind.Operator) && Peek().Text is "<<" or ">>")
        {
            var op = Advance().Text;
            var right = ParseAdditive();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Check(CNodeKind.Operator) && Peek().Text is "+" or "-")
        {
            var op = Advance().Text;
            var right = ParseMultiplicative();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseMultiplicative()
    {
        var left = ParseCast();

        while (Check(CNodeKind.Operator) && Peek().Text is "*" or "/" or "%")
        {
            var op = Advance().Text;
            var right = ParseCast();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode ParseCast()
    {
        if (Check(CNodeKind.Delimiter, "("))
        {
            var saved = _current;
            Advance();

            if (IsTypeSpecifier(Peek()))
            {
                var type = ParseTypeSpecifier();
                Consume(CNodeKind.Delimiter, "OC2049", "期望 ')'");
                var expr = ParseCast();
                return new CCast(type, expr);
            }

            _current = saved;
        }

        return ParseUnary();
    }

    private CAstNode ParseUnary()
    {
        if (Check(CNodeKind.Operator) && Peek().Text is "+" or "-" or "!" or "~" or "*" or "&" or "++" or "--")
        {
            var op = Advance().Text;
            var operand = ParseUnary();
            return new CUnaryOp(op, operand);
        }

        if (Match(CNodeKind.Keyword, "sizeof"))
        {
            if (Match(CNodeKind.Delimiter, "("))
            {
                if (IsTypeSpecifier(Peek()))
                {
                    var type = ParseTypeSpecifier();
                    Consume(CNodeKind.Delimiter, "OC2050", "期望 ')'");
                    return new CSizeOf(type);
                }

                var expr = ParseExpression();
                Consume(CNodeKind.Delimiter, "OC2051", "期望 ')'");
                return new CSizeOf(expr);
            }

            return new CSizeOf(ParseUnary());
        }

        return ParsePostfix();
    }

    private CAstNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
            if (Match(CNodeKind.Delimiter, "["))
            {
                var index = ParseExpression();
                Consume(CNodeKind.Delimiter, "OC2052", "期望 ']'");
                expr = new CSubscript(expr, index);
            }
            else if (Match(CNodeKind.Delimiter, "("))
            {
                var args = new List<CAstNode>();

                if (!Check(CNodeKind.Delimiter, ")"))
                    do
                    {
                        args.Add(ParseExpression());
                    } while (Match(CNodeKind.Delimiter, ","));

                Consume(CNodeKind.Delimiter, "OC2053", "期望 ')'");
                expr = new CCall(expr, args);
            }
            else if (Match(CNodeKind.Operator, "."))
            {
                var member = Consume(CNodeKind.Identifier, "OC2054", "期望成员名").Text;
                expr = new CMemberAccess(expr, member, false);
            }
            else if (Match(CNodeKind.Operator, "->"))
            {
                var member = Consume(CNodeKind.Identifier, "OC2055", "期望成员名").Text;
                expr = new CMemberAccess(expr, member, true);
            }
            else if (Check(CNodeKind.Operator) && Peek().Text is "++" or "--")
            {
                var op = Advance().Text;
                expr = new CUnaryOp(op, expr);
            }
            else
            {
                break;
            }

        return expr;
    }

    private CAstNode ParsePrimary()
    {
        if (Match(CNodeKind.Number))
        {
            var token = Previous();
            return new CLiteral("number", token.Text, default);
        }

        if (Match(CNodeKind.String))
        {
            var token = Previous();
            return new CLiteral("string", token.Text, default);
        }

        if (Match(CNodeKind.Char))
        {
            var token = Previous();
            return new CLiteral("char", token.Text, default);
        }

        if (Check(CNodeKind.Identifier))
        {
            var token = Advance();
            return new CIdentifier(token.Text, default);
        }

        if (Match(CNodeKind.Delimiter, "("))
        {
            var expr = ParseExpression();
            Consume(CNodeKind.Delimiter, "OC2056", "期望 ')'");
            return expr;
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            "OC2057",
            $"意外的标记 '{errorToken.Text}'");

        throw new ParseException($"意外的标记 '{errorToken.Text}'");
    }

    #endregion
}
