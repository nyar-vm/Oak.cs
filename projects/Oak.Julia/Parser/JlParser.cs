using Oak.Diagnostics;
using Oak.Julia.AST;
using Oak.Julia.Lexer;
using Oak.Parsing;
using Oak.Syntax;

namespace Oak.Julia.Parser;

public sealed class JlParser : IParser<IReadOnlyList<JlToken>, JlAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<JlToken> _tokens = [];

    public JlParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public JlAstNode Parse(IReadOnlyList<JlToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var statements = new List<JlAstNode>();

        while (!IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null) statements.Add(stmt);
        }

        return new JlBlock(statements);
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().Type == JlTokenType.Eof;
    }

    private JlToken Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private JlToken Previous()
    {
        return _tokens[_current - 1];
    }

    private JlToken Advance()
    {
        if (!IsAtEnd()) _current++;

        return Previous();
    }

    private bool Check(JlTokenType type)
    {
        return !IsAtEnd() && Peek().Type == type;
    }

    private bool Check(JlTokenType type, string value)
    {
        return !IsAtEnd() && Peek().Type == type && Peek().Value == value;
    }

    private bool Match(JlTokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(JlTokenType type, string value)
    {
        if (Check(type, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private JlToken Consume(JlTokenType type, string errorCode, string message)
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

    private JlToken ConsumeKeyword(string keyword, string errorCode, string message)
    {
        if (Check(JlTokenType.Keyword, keyword)) return Advance();

        var token = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Peek().Type == JlTokenType.Keyword)
            {
                var value = Peek().Value;
                if (value is "function" or "macro" or "struct" or "module"
                    or "if" or "for" or "while" or "try" or "begin"
                    or "import" or "using" or "export")
                    return;
            }

            Advance();
        }
    }

    #endregion

    #region Statements

    private JlAstNode? ParseStatement()
    {
        try
        {
            if (Check(JlTokenType.Keyword, "module")) return ParseModule();

            if (Check(JlTokenType.Keyword, "import") || Check(JlTokenType.Keyword, "using"))
                return ParseImport();

            if (Check(JlTokenType.Keyword, "export")) return ParseExport();

            if (Check(JlTokenType.Keyword, "function")) return ParseFunctionDef();

            if (Check(JlTokenType.Keyword, "macro")) return ParseMacroDef();

            if (Check(JlTokenType.Keyword, "struct") || Check(JlTokenType.Keyword, "abstract"))
                return ParseStructDef();

            if (Check(JlTokenType.Keyword, "if")) return ParseIfExpr();

            if (Check(JlTokenType.Keyword, "for")) return ParseForExpr();

            if (Check(JlTokenType.Keyword, "while")) return ParseWhileExpr();

            if (Check(JlTokenType.Keyword, "try")) return ParseTryExpr();

            if (Check(JlTokenType.Keyword, "let")) return ParseLetExpr();

            if (Check(JlTokenType.Keyword, "return")) return ParseReturnExpr();

            if (Check(JlTokenType.Keyword, "break"))
            {
                Advance();
                return new JlBreakExpr();
            }

            if (Check(JlTokenType.Keyword, "continue"))
            {
                Advance();
                return new JlContinueExpr();
            }

            if (Check(JlTokenType.Keyword, "begin")) return ParseBlock();

            return ParseExprStatement();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private JlModule ParseModule()
    {
        ConsumeKeyword("module", "JLA2001", "期望 'module' 关键字");
        var name = Consume(JlTokenType.Identifier, "JLA2002", "期望模块名").Value;

        var statements = new List<JlAstNode>();

        while (!Check(JlTokenType.Keyword, "end") && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null) statements.Add(stmt);
        }

        ConsumeKeyword("end", "JLA2003", "期望 'end'");

        return new JlModule(name, [], [], statements);
    }

    private JlImport ParseImport()
    {
        var isUsing = Match(JlTokenType.Keyword, "using");
        if (!isUsing) ConsumeKeyword("import", "JLA2004", "期望 'import' 或 'using'");

        var moduleBuilder = new System.Text.StringBuilder();
        moduleBuilder.Append(Consume(JlTokenType.Identifier, "JLA2005", "期望模块名").Value);

        while (Match(JlTokenType.Operator, "."))
            moduleBuilder.Append('.').Append(Consume(JlTokenType.Identifier, "JLA2006", "期望标识符").Value);

        var moduleName = moduleBuilder.ToString();

        var names = new List<string>();
        if (Match(JlTokenType.Operator, ":"))
        {
            names.Add(Consume(JlTokenType.Identifier, "JLA2007", "期望导入名").Value);
            while (Match(JlTokenType.Delimiter, ","))
                names.Add(Consume(JlTokenType.Identifier, "JLA2008", "期望导入名").Value);
        }

        return new JlImport(isUsing, moduleName, names);
    }

    private JlExport ParseExport()
    {
        ConsumeKeyword("export", "JLA2009", "期望 'export' 关键字");

        var names = new List<string>();
        names.Add(Consume(JlTokenType.Identifier, "JLA2010", "期望导出名").Value);
        while (Match(JlTokenType.Delimiter, ","))
            names.Add(Consume(JlTokenType.Identifier, "JLA2011", "期望导出名").Value);

        return new JlExport(names);
    }

    private JlFunctionDef ParseFunctionDef()
    {
        ConsumeKeyword("function", "JLA2012", "期望 'function' 关键字");

        var name = Consume(JlTokenType.Identifier, "JLA2013", "期望函数名").Value;

        Consume(JlTokenType.Delimiter, "JLA2014", "期望 '('");
        var parameters = ParseParameters();
        Consume(JlTokenType.Delimiter, "JLA2015", "期望 ')'");

        JlAstNode? returnType = null;
        if (Match(JlTokenType.Operator, "::")) returnType = ParseType();

        var whereParams = new List<string>();
        if (Match(JlTokenType.Keyword, "where"))
        {
            whereParams.Add(Consume(JlTokenType.Identifier, "JLA2016", "期望类型参数").Value);
            while (Match(JlTokenType.Delimiter, ","))
                whereParams.Add(Consume(JlTokenType.Identifier, "JLA2017", "期望类型参数").Value);
        }

        var body = ParseBlockBody();

        return new JlFunctionDef(name, parameters, whereParams, returnType, body, false);
    }

    private JlMacroDef ParseMacroDef()
    {
        ConsumeKeyword("macro", "JLA2018", "期望 'macro' 关键字");

        var name = Consume(JlTokenType.Identifier, "JLA2019", "期望宏名").Value;

        Consume(JlTokenType.Delimiter, "JLA2020", "期望 '('");
        var parameters = ParseParameters();
        Consume(JlTokenType.Delimiter, "JLA2021", "期望 ')'");

        var body = ParseBlockBody();

        return new JlMacroDef(name, parameters, body);
    }

    private JlAstNode ParseStructDef()
    {
        var isAbstract = Match(JlTokenType.Keyword, "abstract");

        if (isAbstract)
        {
            ConsumeKeyword("type", "JLA2022", "期望 'type' 关键字");
            var name = Consume(JlTokenType.Identifier, "JLA2023", "期望类型名").Value;

            var typeParams = new List<string>();
            if (Match(JlTokenType.Delimiter, "{"))
            {
                typeParams.Add(Consume(JlTokenType.Identifier, "JLA2024", "期望类型参数").Value);
                while (Match(JlTokenType.Delimiter, ","))
                    typeParams.Add(Consume(JlTokenType.Identifier, "JLA2025", "期望类型参数").Value);
                Consume(JlTokenType.Delimiter, "JLA2026", "期望 '}'");
            }

            JlAstNode? superType = null;
            if (Match(JlTokenType.Operator, "<:")) superType = ParseType();

            ConsumeKeyword("end", "JLA2027", "期望 'end'");

            return new JlTypeDef(name, typeParams, superType);
        }

        var isMutable = Match(JlTokenType.Keyword, "mutable");
        if (isMutable) ConsumeKeyword("struct", "JLA2028", "期望 'struct' 关键字");
        else ConsumeKeyword("struct", "JLA2029", "期望 'struct' 关键字");

        var structName = Consume(JlTokenType.Identifier, "JLA2030", "期望结构体名").Value;

        var structTypeParams = new List<string>();
        if (Match(JlTokenType.Delimiter, "{"))
        {
            structTypeParams.Add(Consume(JlTokenType.Identifier, "JLA2031", "期望类型参数").Value);
            while (Match(JlTokenType.Delimiter, ","))
                structTypeParams.Add(Consume(JlTokenType.Identifier, "JLA2032", "期望类型参数").Value);
            Consume(JlTokenType.Delimiter, "JLA2033", "期望 '}'");
        }

        JlAstNode? structSuperType = null;
        if (Match(JlTokenType.Operator, "<:")) structSuperType = ParseType();

        var fields = new List<JlAstNode>();

        while (!Check(JlTokenType.Keyword, "end") && !IsAtEnd())
        {
            var fieldName = Consume(JlTokenType.Identifier, "JLA2034", "期望字段名").Value;

            JlAstNode? fieldType = null;
            if (Match(JlTokenType.Operator, "::")) fieldType = ParseType();

            fields.Add(new JlField(fieldName, fieldType));
        }

        ConsumeKeyword("end", "JLA2035", "期望 'end'");

        return new JlStructDef(structName, structTypeParams, fields, isMutable, false);
    }

    private IReadOnlyList<JlAstNode> ParseParameters()
    {
        var parameters = new List<JlAstNode>();

        while (!Check(JlTokenType.Delimiter, ")") && !IsAtEnd())
        {
            var isVarargs = Match(JlTokenType.Operator, "...");

            var name = Consume(JlTokenType.Identifier, "JLA2036", "期望参数名").Value;

            JlAstNode? typeAnnotation = null;
            if (Match(JlTokenType.Operator, "::")) typeAnnotation = ParseType();

            JlAstNode? defaultValue = null;
            if (Match(JlTokenType.Operator, "=")) defaultValue = ParseExpression();

            parameters.Add(new JlParameter(name, typeAnnotation, defaultValue, isVarargs));

            if (!Match(JlTokenType.Delimiter, ",")) break;
        }

        return parameters;
    }

    private JlBlock ParseBlockBody()
    {
        var statements = new List<JlAstNode>();

        while (!Check(JlTokenType.Keyword, "end") && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null) statements.Add(stmt);
        }

        ConsumeKeyword("end", "JLA2037", "期望 'end'");

        return new JlBlock(statements);
    }

    private JlIfExpr ParseIfExpr()
    {
        ConsumeKeyword("if", "JLA2038", "期望 'if' 关键字");
        var condition = ParseExpression();

        var thenBranch = ParseBlockOrStatement();

        var elseIfBranches = new List<(JlAstNode Condition, JlAstNode Body)>();

        while (Check(JlTokenType.Keyword, "elseif"))
        {
            Advance();
            var elseIfCond = ParseExpression();
            var elseIfBody = ParseBlockOrStatement();
            elseIfBranches.Add((elseIfCond, elseIfBody));
        }

        JlAstNode? elseBranch = null;
        if (Match(JlTokenType.Keyword, "else")) elseBranch = ParseBlockOrStatement();

        ConsumeKeyword("end", "JLA2039", "期望 'end'");

        return new JlIfExpr(condition, thenBranch, elseIfBranches, elseBranch);
    }

    private JlForExpr ParseForExpr()
    {
        ConsumeKeyword("for", "JLA2040", "期望 'for' 关键字");

        var iterators = new List<(JlAstNode Iterator, JlAstNode Iterable)>();
        iterators.Add(ParseIterator());

        while (Match(JlTokenType.Delimiter, ",")) iterators.Add(ParseIterator());

        var body = ParseBlockOrStatement();

        ConsumeKeyword("end", "JLA2041", "期望 'end'");

        return new JlForExpr(iterators, body);
    }

    private (JlAstNode Iterator, JlAstNode Iterable) ParseIterator()
    {
        var iterator = ParseExpression();
        ConsumeKeyword("in", "JLA2042", "期望 'in' 关键字");
        var iterable = ParseExpression();

        return (iterator, iterable);
    }

    private JlWhileExpr ParseWhileExpr()
    {
        ConsumeKeyword("while", "JLA2043", "期望 'while' 关键字");
        var condition = ParseExpression();
        var body = ParseBlockOrStatement();

        ConsumeKeyword("end", "JLA2044", "期望 'end'");

        return new JlWhileExpr(condition, body);
    }

    private JlTryExpr ParseTryExpr()
    {
        ConsumeKeyword("try", "JLA2045", "期望 'try' 关键字");
        var body = ParseBlockOrStatement();

        var catchClauses = new List<(JlAstNode Pattern, JlAstNode Body)>();

        if (Match(JlTokenType.Keyword, "catch"))
        {
            JlAstNode pattern = new JlIdentifier("_");
            if (Check(JlTokenType.Identifier)) pattern = new JlIdentifier(Advance().Value);

            var catchBody = ParseBlockOrStatement();
            catchClauses.Add((pattern, catchBody));
        }

        JlAstNode? finallyBody = null;
        if (Match(JlTokenType.Keyword, "finally")) finallyBody = ParseBlockOrStatement();

        ConsumeKeyword("end", "JLA2046", "期望 'end'");

        return new JlTryExpr(body, catchClauses, finallyBody);
    }

    private JlLetExpr ParseLetExpr()
    {
        ConsumeKeyword("let", "JLA2047", "期望 'let' 关键字");

        var bindings = new List<JlAstNode>();
        while (!Check(JlTokenType.Keyword, "end") && !IsAtEnd() && !Check(JlTokenType.Delimiter, ";"))
        {
            var name = Consume(JlTokenType.Identifier, "JLA2048", "期望绑定名").Value;

            JlAstNode? typeAnnotation = null;
            if (Match(JlTokenType.Operator, "::")) typeAnnotation = ParseType();

            JlAstNode? value = null;
            if (Match(JlTokenType.Operator, "=")) value = ParseExpression();

            bindings.Add(new JlAssignment(new JlIdentifier(name), value ?? new JlUnit()));

            if (!Match(JlTokenType.Delimiter, ",")) break;
        }

        Match(JlTokenType.Delimiter, ";");

        var body = ParseBlockOrStatement();

        ConsumeKeyword("end", "JLA2049", "期望 'end'");

        return new JlLetExpr(bindings, body);
    }

    private JlReturnExpr ParseReturnExpr()
    {
        ConsumeKeyword("return", "JLA2050", "期望 'return' 关键字");

        JlAstNode? value = null;
        if (!Check(JlTokenType.Keyword, "end") && !Check(JlTokenType.Delimiter, ";") && !IsAtEnd())
            value = ParseExpression();

        return new JlReturnExpr(value);
    }

    private JlBlock ParseBlock()
    {
        ConsumeKeyword("begin", "JLA2051", "期望 'begin' 关键字");

        var statements = new List<JlAstNode>();

        while (!Check(JlTokenType.Keyword, "end") && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null) statements.Add(stmt);
        }

        ConsumeKeyword("end", "JLA2052", "期望 'end'");

        return new JlBlock(statements);
    }

    private JlAstNode ParseBlockOrStatement()
    {
        if (Check(JlTokenType.Keyword, "begin")) return ParseBlock();

        var statements = new List<JlAstNode>();

        while (!Check(JlTokenType.Keyword, "end") && !Check(JlTokenType.Keyword, "elseif") &&
               !Check(JlTokenType.Keyword, "else") && !Check(JlTokenType.Keyword, "catch") &&
               !Check(JlTokenType.Keyword, "finally") && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null) statements.Add(stmt);

            Match(JlTokenType.Delimiter, ";");
        }

        if (statements.Count == 1) return statements[0];

        return new JlBlock(statements);
    }

    private JlAstNode ParseExprStatement()
    {
        var expr = ParseExpression();

        if (Match(JlTokenType.Operator, "="))
        {
            var right = ParseExpression();
            return new JlAssignment(expr, right);
        }

        var compoundOps = new[] { "+=", "-=", "*=", "/=", "//=", "^=", "%=" };
        foreach (var op in compoundOps)
        {
            if (Match(JlTokenType.Operator, op))
            {
                var right = ParseExpression();
                return new JlCompoundAssignment(expr, op, right);
            }
        }

        return expr;
    }

    #endregion

    #region Expressions

    private JlAstNode ParseExpression()
    {
        return ParseAssignment();
    }

    private JlAstNode ParseAssignment()
    {
        var expr = ParseTernary();

        if (Check(JlTokenType.Operator, "="))
        {
            Advance();
            var right = ParseAssignment();
            return new JlAssignment(expr, right);
        }

        return expr;
    }

    private JlAstNode ParseTernary()
    {
        var expr = ParseOr();

        if (Match(JlTokenType.Operator, "?"))
        {
            var thenBranch = ParseExpression();
            Consume(JlTokenType.Punctuation, "JLA2053", "期望 ':'");
            var elseBranch = ParseTernary();
            return new JlTernary(expr, thenBranch, elseBranch);
        }

        return expr;
    }

    private JlAstNode ParseOr()
    {
        var left = ParseAnd();

        while (Match(JlTokenType.Operator, "||"))
        {
            var right = ParseAnd();
            left = new JlBinaryOp(left, "||", right);
        }

        return left;
    }

    private JlAstNode ParseAnd()
    {
        var left = ParseComparison();

        while (Match(JlTokenType.Operator, "&&"))
        {
            var right = ParseComparison();
            left = new JlBinaryOp(left, "&&", right);
        }

        return left;
    }

    private JlAstNode ParseComparison()
    {
        var left = ParsePipe();

        while (Check(JlTokenType.Operator) &&
               Peek().Value is "==" or "!=" or "<" or ">" or "<=" or ">=" or "===" or "!==" or "isa")
        {
            var op = Advance().Value;
            var right = ParsePipe();
            left = new JlBinaryOp(left, op, right);
        }

        return left;
    }

    private JlAstNode ParsePipe()
    {
        var left = ParseRange();

        while (Check(JlTokenType.Operator, "|>") || Check(JlTokenType.Operator, "<|"))
        {
            var isReverse = Advance().Value == "<|";
            var right = ParseRange();
            left = new JlPipe(left, right, isReverse);
        }

        return left;
    }

    private JlAstNode ParseRange()
    {
        var left = ParseAdditive();

        if (Check(JlTokenType.Operator, "..") || Check(JlTokenType.Operator, ":"))
        {
            Advance();
            var end = ParseAdditive();
            return new JlRange(left, null, end);
        }

        return left;
    }

    private JlAstNode ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Check(JlTokenType.Operator) && Peek().Value is "+" or "-")
        {
            var op = Advance().Value;
            var right = ParseMultiplicative();
            left = new JlBinaryOp(left, op, right);
        }

        return left;
    }

    private JlAstNode ParseMultiplicative()
    {
        var left = ParseUnary();

        while (Check(JlTokenType.Operator) && Peek().Value is "*" or "/" or "//" or "%" or "\\" or "÷")
        {
            var op = Advance().Value;
            var right = ParseUnary();
            left = new JlBinaryOp(left, op, right);
        }

        return left;
    }

    private JlAstNode ParseUnary()
    {
        if (Check(JlTokenType.Operator, "-") || Check(JlTokenType.Operator, "!"))
        {
            var op = Advance().Value;
            var operand = ParseUnary();
            return new JlUnaryOp(op, operand, true);
        }

        return ParsePower();
    }

    private JlAstNode ParsePower()
    {
        var left = ParsePostfix();

        if (Match(JlTokenType.Operator, "^"))
        {
            var right = ParseUnary();
            left = new JlBinaryOp(left, "^", right);
        }

        return left;
    }

    private JlAstNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Check(JlTokenType.Delimiter, "("))
            {
                Advance();
                var args = new List<JlAstNode>();
                var kwargs = new List<JlAstNode>();

                if (!Check(JlTokenType.Delimiter, ")"))
                {
                    args.Add(ParseExpression());
                    while (Match(JlTokenType.Delimiter, ",")) args.Add(ParseExpression());
                }

                Consume(JlTokenType.Delimiter, "JLA2054", "期望 ')'");

                expr = new JlCall(expr, args, kwargs);
            }
            else if (Check(JlTokenType.Delimiter, "["))
            {
                Advance();
                var indices = new List<JlAstNode>();

                if (!Check(JlTokenType.Delimiter, "]"))
                {
                    indices.Add(ParseExpression());
                    while (Match(JlTokenType.Delimiter, ",")) indices.Add(ParseExpression());
                }

                Consume(JlTokenType.Delimiter, "JLA2055", "期望 ']'");
                expr = new JlIndex(expr, indices);
            }
            else if (Match(JlTokenType.Operator, "."))
            {
                var field = Consume(JlTokenType.Identifier, "JLA2056", "期望字段名").Value;
                expr = new JlFieldAccess(expr, field);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private JlAstNode ParsePrimary()
    {
        if (Check(JlTokenType.Number))
        {
            var token = Advance();
            return new JlLiteral("number", token.Value);
        }

        if (Check(JlTokenType.String))
        {
            var token = Advance();
            return new JlLiteral("string", token.Value);
        }

        if (Check(JlTokenType.Char))
        {
            var token = Advance();
            return new JlLiteral("char", token.Value);
        }

        if (Check(JlTokenType.Keyword, "true"))
        {
            Advance();
            return new JlLiteral("bool", "true");
        }

        if (Check(JlTokenType.Keyword, "false"))
        {
            Advance();
            return new JlLiteral("bool", "false");
        }

        if (Check(JlTokenType.Keyword, "nothing"))
        {
            Advance();
            return new JlUnit();
        }

        if (Check(JlTokenType.Identifier))
        {
            var token = Advance();
            return new JlIdentifier(token.Value);
        }

        if (Check(JlTokenType.MacroName))
        {
            var token = Advance();
            return new JlMacroCall(token.Value, []);
        }

        if (Check(JlTokenType.Symbol))
        {
            var token = Advance();
            return new JlSymbol(token.Value);
        }

        if (Check(JlTokenType.CommandType))
        {
            var token = Advance();
            return new JlCommand(token.Value);
        }

        if (Check(JlTokenType.Delimiter, "("))
        {
            Advance();

            if (Match(JlTokenType.Delimiter, ")")) return new JlTuple([]);

            var expr = ParseExpression();

            if (Check(JlTokenType.Delimiter, ","))
            {
                var elements = new List<JlAstNode> { expr };
                while (Match(JlTokenType.Delimiter, ",")) elements.Add(ParseExpression());
                Consume(JlTokenType.Delimiter, "JLA2057", "期望 ')'");
                return new JlTuple(elements);
            }

            Consume(JlTokenType.Delimiter, "JLA2058", "期望 ')'");
            return expr;
        }

        if (Check(JlTokenType.Delimiter, "["))
        {
            Advance();

            if (Match(JlTokenType.Delimiter, "]")) return new JlArray([]);

            var elements = new List<JlAstNode>();
            elements.Add(ParseExpression());
            while (Match(JlTokenType.Delimiter, ",")) elements.Add(ParseExpression());

            Consume(JlTokenType.Delimiter, "JLA2059", "期望 ']'");
            return new JlArray(elements);
        }

        if (Check(JlTokenType.Operator, "->"))
        {
            Advance();
            var parameters = new List<JlAstNode>();
            var body = ParseExpression();
            return new JlLambda(parameters, body);
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            "JLA2060",
            $"意外的标记 '{errorToken.Value}'");

        throw new ParseException($"意外的标记 '{errorToken.Value}'");
    }

    #endregion

    #region Types

    private JlAstNode ParseType()
    {
        if (Check(JlTokenType.Delimiter, "{"))
        {
            Advance();
            var args = new List<JlAstNode>();
            args.Add(ParseType());
            while (Match(JlTokenType.Delimiter, ",")) args.Add(ParseType());
            Consume(JlTokenType.Delimiter, "JLA2061", "期望 '}'");

            if (Check(JlTokenType.Identifier))
            {
                var name = Advance().Value;
                return new JlTypeApp(new JlTypeCon(name), args);
            }

            return new JlTupleType(args);
        }

        if (Check(JlTokenType.Identifier))
        {
            var name = Advance().Value;

            if (char.IsLower(name[0])) return new JlTypeVar(name);

            if (Check(JlTokenType.Delimiter, "{"))
            {
                Advance();
                var args = new List<JlAstNode>();
                args.Add(ParseType());
                while (Match(JlTokenType.Delimiter, ",")) args.Add(ParseType());
                Consume(JlTokenType.Delimiter, "JLA2062", "期望 '}'");
                return new JlTypeApp(new JlTypeCon(name), args);
            }

            return new JlTypeCon(name);
        }

        if (Check(JlTokenType.Delimiter, "("))
        {
            Advance();
            var type = ParseType();
            Consume(JlTokenType.Delimiter, "JLA2063", "期望 ')'");
            return type;
        }

        return new JlTypeCon("Any");
    }

    #endregion
}
