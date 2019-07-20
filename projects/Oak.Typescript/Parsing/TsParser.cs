using Oak.Diagnostics;
using Oak.Parsing;
using Oak.Typescript.AST;
using Oak.Typescript.Lexer;
using Oak.Syntax;

namespace Oak.Typescript.Parsing;

/// <summary>
///     TypeScript 语法分析器
/// </summary>
public sealed class TsParser : IParser<IReadOnlyList<TsToken>, TsAstNode>
{
    private readonly string _filePath = string.Empty;
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<TsToken> _tokens = [];

    /// <summary>
    ///     创建 TypeScript 语法分析器
    /// </summary>
    public TsParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     解析词法单元序列
    /// </summary>
    public TsAstNode Parse(IReadOnlyList<TsToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var declarations = new List<TsAstNode>();

        while (!IsAtEnd())
        {
            var decl = ParseDeclaration();
            if (decl is not null) declarations.Add(decl);
        }

        return new TsCompilationUnit(declarations);
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().Type == TsTokenType.Eof;
    }

    private TsToken Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private TsToken PeekNext()
    {
        var index = _current + 1;
        return index < _tokens.Count ? _tokens[index] : _tokens[^1];
    }

    private TsToken Previous()
    {
        return _tokens[_current - 1];
    }

    private TsToken Advance()
    {
        if (!IsAtEnd()) _current++;

        return Previous();
    }

    private bool Check(TsTokenType type)
    {
        return !IsAtEnd() && Peek().Type == type;
    }

    private bool Check(TsTokenType type, string value)
    {
        return !IsAtEnd() && Peek().Type == type && Peek().Value == value;
    }

    private bool Match(TsTokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(TsTokenType type, string value)
    {
        if (Check(type, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private TsToken Consume(TsTokenType type, string errorCode, string message)
    {
        if (Check(type)) return Advance();

        var token = Peek();
        _diagnostics?.AddError(
            _filePath,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private TsToken ConsumeKeyword(string keyword, string errorCode, string message)
    {
        if (Check(TsTokenType.Keyword, keyword)) return Advance();

        var token = Peek();
        _diagnostics?.AddError(
            _filePath,
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
            if (Previous().Type == TsTokenType.Delimiter && Previous().Value == ";") return;

            if (Peek().Type == TsTokenType.Keyword)
                switch (Peek().Value)
                {
                    case "const":
                    case "let":
                    case "var":
                    case "function":
                    case "class":
                    case "interface":
                    case "type":
                    case "import":
                    case "export":
                    case "if":
                    case "for":
                    case "while":
                    case "do":
                    case "switch":
                    case "try":
                    case "return":
                    case "throw":
                    case "break":
                    case "continue":
                    case "enum":
                    case "namespace":
                        return;
                }

            if (Peek().Type == TsTokenType.Delimiter && Peek().Value == "}") return;

            Advance();
        }
    }

    #endregion

    #region Declarations

    private TsAstNode? ParseDeclaration()
    {
        try
        {
            if (Check(TsTokenType.Keyword, "import")) return ParseImportDecl();

            if (Check(TsTokenType.Keyword, "export")) return ParseExportDecl();

            if (Check(TsTokenType.Keyword, "const") || Check(TsTokenType.Keyword, "let") ||
                Check(TsTokenType.Keyword, "var"))
                return ParseVariableDecl();

            if (Check(TsTokenType.Keyword, "function")) return ParseFunctionDecl();

            if (Check(TsTokenType.Keyword, "class")) return ParseClassDecl();

            if (Check(TsTokenType.Keyword, "interface")) return ParseInterfaceDecl();

            if (Check(TsTokenType.Keyword, "type")) return ParseTypeAliasDecl();

            if (Check(TsTokenType.Keyword, "enum")) return ParseEnumDecl();

            if (Check(TsTokenType.Keyword, "namespace")) return ParseNamespaceDecl();

            return ParseStatement();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private TsImportDecl ParseImportDecl()
    {
        ConsumeKeyword("import", "OAK2001", "期望 'import' 关键字");

        string? alias = null;
        var isTypeOnly = false;

        if (Match(TsTokenType.Keyword, "type")) isTypeOnly = true;

        string modulePath;

        if (Match(TsTokenType.Delimiter, "{"))
        {
            while (!Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
            {
                Advance();

                if (Match(TsTokenType.Delimiter, ","))
                {
                }
            }

            Consume(TsTokenType.Delimiter, "OAK2002", "期望 '}'");
            ConsumeKeyword("from", "OAK2003", "期望 'from' 关键字");
            modulePath = Consume(TsTokenType.String, "OAK2004", "期望模块路径字符串").Value;
        }
        else if (Match(TsTokenType.Operator, "*"))
        {
            ConsumeKeyword("as", "OAK2005", "期望 'as' 关键字");
            alias = Consume(TsTokenType.Identifier, "OAK2006", "期望别名").Value;
            ConsumeKeyword("from", "OAK2007", "期望 'from' 关键字");
            modulePath = Consume(TsTokenType.String, "OAK2008", "期望模块路径字符串").Value;
        }
        else
        {
            alias = Consume(TsTokenType.Identifier, "OAK2009", "期望导入名").Value;
            ConsumeKeyword("from", "OAK2010", "期望 'from' 关键字");
            modulePath = Consume(TsTokenType.String, "OAK2011", "期望模块路径字符串").Value;
        }

        Match(TsTokenType.Delimiter, ";");

        return new TsImportDecl(modulePath, alias, isTypeOnly);
    }

    private TsExportDecl ParseExportDecl()
    {
        ConsumeKeyword("export", "OAK2012", "期望 'export' 关键字");
        var decl = ParseDeclaration();

        if (decl is null) throw new ParseException("导出后应为声明");

        var name = decl switch
        {
            TsVariableDecl v => v.Name,
            TsFunctionDecl f => f.Name,
            TsClassDecl c => c.Name,
            TsInterfaceDecl i => i.Name,
            TsTypeAliasDecl t => t.Name,
            TsEnumDecl e => e.Name,
            TsNamespaceDecl n => n.Name,
            _ => "default"
        };

        return new TsExportDecl(name, decl);
    }

    private TsVariableDecl ParseVariableDecl()
    {
        var isConst = Match(TsTokenType.Keyword, "const");

        if (!isConst)
        {
            if (Check(TsTokenType.Keyword, "let"))
                Advance();
            else if (Check(TsTokenType.Keyword, "var")) Advance();
        }

        var name = Consume(TsTokenType.Identifier, "OAK2013", "期望变量名").Value;

        TsAstNode? typeAnnotation = null;
        if (Match(TsTokenType.Punctuation, ":")) typeAnnotation = ParseTypeAnnotation();

        TsAstNode? initializer = null;
        if (Match(TsTokenType.Operator, "=")) initializer = ParseAssignment();

        Match(TsTokenType.Delimiter, ";");

        return new TsVariableDecl(name, typeAnnotation, initializer, isConst);
    }

    private TsFunctionDecl ParseFunctionDecl()
    {
        var isAsync = Match(TsTokenType.Keyword, "async");

        ConsumeKeyword("function", "OAK2014", "期望 'function' 关键字");

        var isGenerator = Match(TsTokenType.Operator, "*");

        var name = Consume(TsTokenType.Identifier, "OAK2015", "期望函数名").Value;

        Consume(TsTokenType.Delimiter, "OAK2016", "期望 '('");
        var parameters = ParseParameters();
        Consume(TsTokenType.Delimiter, "OAK2017", "期望 ')'");

        TsAstNode? returnType = null;
        if (Match(TsTokenType.Punctuation, ":")) returnType = ParseTypeAnnotation();

        var body = ParseBlockStmt();

        return new TsFunctionDecl(name, parameters, returnType, body, isAsync, isGenerator);
    }

    private TsClassDecl ParseClassDecl()
    {
        ConsumeKeyword("class", "OAK2018", "期望 'class' 关键字");
        var name = Consume(TsTokenType.Identifier, "OAK2019", "期望类名").Value;

        Consume(TsTokenType.Delimiter, "OAK2020", "期望 '{'");

        var members = new List<TsAstNode>();

        while (!Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
        {
            var member = ParseClassMember();
            if (member is not null) members.Add(member);
        }

        Consume(TsTokenType.Delimiter, "OAK2021", "期望 '}'");

        return new TsClassDecl(name, members);
    }

    private TsAstNode? ParseClassMember()
    {
        try
        {
            if (Check(TsTokenType.Keyword, "get") || Check(TsTokenType.Keyword, "set")) Advance();

            var name = Consume(TsTokenType.Identifier, "OAK2022", "期望成员名").Value;

            if (Check(TsTokenType.Delimiter, "(")) return ParseMethodDecl(name);

            TsAstNode? typeAnnotation = null;
            if (Match(TsTokenType.Punctuation, ":")) typeAnnotation = ParseTypeAnnotation();

            TsAstNode? initializer = null;
            if (Match(TsTokenType.Operator, "=")) initializer = ParseExpression();

            Match(TsTokenType.Delimiter, ";");

            return new TsVariableDecl(name, typeAnnotation, initializer, false);
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private TsFunctionDecl ParseMethodDecl(string name)
    {
        Consume(TsTokenType.Delimiter, "OAK2023", "期望 '('");
        var parameters = ParseParameters();
        Consume(TsTokenType.Delimiter, "OAK2024", "期望 ')'");

        TsAstNode? returnType = null;
        if (Match(TsTokenType.Punctuation, ":")) returnType = ParseTypeAnnotation();

        var body = ParseBlockStmt();

        return new TsFunctionDecl(name, parameters, returnType, body, false, false);
    }

    private TsInterfaceDecl ParseInterfaceDecl()
    {
        ConsumeKeyword("interface", "OAK2025", "期望 'interface' 关键字");
        var name = Consume(TsTokenType.Identifier, "OAK2026", "期望接口名").Value;

        Consume(TsTokenType.Delimiter, "OAK2027", "期望 '{'");

        var members = new List<TsAstNode>();

        while (!Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
        {
            var memberName = Consume(TsTokenType.Identifier, "OAK2028", "期望属性名").Value;

            Match(TsTokenType.Operator, "?");

            Consume(TsTokenType.Punctuation, "OAK2029", "期望 ':'");
            var memberType = ParseTypeAnnotation();

            Match(TsTokenType.Delimiter, ";");

            members.Add(new TsVariableDecl(memberName, memberType, null, false));
        }

        Consume(TsTokenType.Delimiter, "OAK2030", "期望 '}'");

        return new TsInterfaceDecl(name, members);
    }

    private TsTypeAliasDecl ParseTypeAliasDecl()
    {
        ConsumeKeyword("type", "OAK2031", "期望 'type' 关键字");
        var name = Consume(TsTokenType.Identifier, "OAK2032", "期望类型别名").Value;

        Consume(TsTokenType.Operator, "OAK2033", "期望 '='");
        var type = ParseTypeAnnotation();
        Match(TsTokenType.Delimiter, ";");

        return new TsTypeAliasDecl(name, type);
    }

    private TsEnumDecl ParseEnumDecl()
    {
        ConsumeKeyword("enum", "OAK2070", "期望 'enum' 关键字");

        var isConst = false;
        if (Check(TsTokenType.Keyword, "const"))
        {
            Advance();
            isConst = true;
        }

        var name = Consume(TsTokenType.Identifier, "OAK2071", "期望枚举名").Value;

        Consume(TsTokenType.Delimiter, "OAK2072", "期望 '{'");

        var members = new List<TsEnumMember>();

        while (!Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
        {
            var memberName = Consume(TsTokenType.Identifier, "OAK2073", "期望枚举成员名").Value;

            TsAstNode? initializer = null;
            if (Match(TsTokenType.Operator, "=")) initializer = ParseAssignment();

            members.Add(new TsEnumMember(memberName, initializer));

            if (!Match(TsTokenType.Delimiter, ",")) break;
        }

        Consume(TsTokenType.Delimiter, "OAK2074", "期望 '}'");

        return new TsEnumDecl(name, members, isConst);
    }

    private TsNamespaceDecl ParseNamespaceDecl()
    {
        ConsumeKeyword("namespace", "OAK2075", "期望 'namespace' 关键字");
        var name = Consume(TsTokenType.Identifier, "OAK2076", "期望命名空间名").Value;

        Consume(TsTokenType.Delimiter, "OAK2077", "期望 '{'");

        var members = new List<TsAstNode>();

        while (!Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
        {
            var decl = ParseDeclaration();
            if (decl is not null) members.Add(decl);
        }

        Consume(TsTokenType.Delimiter, "OAK2078", "期望 '}'");

        return new TsNamespaceDecl(name, members);
    }

    #endregion

    #region Parameters

    private IReadOnlyList<TsParameter> ParseParameters()
    {
        var parameters = new List<TsParameter>();

        if (!Check(TsTokenType.Delimiter, ")"))
        {
            parameters.Add(ParseParameter());

            while (Match(TsTokenType.Delimiter, ",")) parameters.Add(ParseParameter());
        }

        return parameters;
    }

    private TsParameter ParseParameter()
    {
        if (Match(TsTokenType.Operator, "..."))
        {
            var restName = Consume(TsTokenType.Identifier, "OAK2034", "期望参数名").Value;

            TsAstNode? restTypeAnnotation = null;
            if (Match(TsTokenType.Punctuation, ":")) restTypeAnnotation = ParseTypeAnnotation();

            return new TsParameter($"...{restName}", restTypeAnnotation, null);
        }

        var name = Consume(TsTokenType.Identifier, "OAK2034", "期望参数名").Value;

        TsAstNode? typeAnnotation = null;
        if (Match(TsTokenType.Punctuation, ":")) typeAnnotation = ParseTypeAnnotation();

        TsAstNode? defaultValue = null;
        if (Match(TsTokenType.Operator, "=")) defaultValue = ParseAssignment();

        return new TsParameter(name, typeAnnotation, defaultValue);
    }

    #endregion

    #region Types

    private TsAstNode ParseTypeAnnotation()
    {
        var types = new List<TsAstNode> { ParseSingleType() };

        while (Match(TsTokenType.Operator, "|")) types.Add(ParseSingleType());

        if (types.Count == 1) return new TsTypeAnnotation(types[0]);

        return new TsTypeAnnotation(new TsUnionType(types));
    }

    private TsAstNode ParseSingleType()
    {
        if (Check(TsTokenType.Keyword))
        {
            var name = Advance().Value;
            return new TsPrimitiveType(name);
        }

        if (Check(TsTokenType.Identifier))
        {
            var name = Advance().Value;
            return new TsPrimitiveType(name);
        }

        if (Match(TsTokenType.Delimiter, "["))
        {
            Consume(TsTokenType.Delimiter, "OAK2035", "期望 ']'");
            return new TsArrayType(new TsPrimitiveType("any"));
        }

        if (Match(TsTokenType.Delimiter, "{"))
        {
            while (!Check(TsTokenType.Delimiter, "}") && !IsAtEnd()) Advance();

            Consume(TsTokenType.Delimiter, "OAK2036", "期望 '}'");
            return new TsPrimitiveType("object");
        }

        return new TsPrimitiveType("any");
    }

    #endregion

    #region Statements

    private TsAstNode ParseStatement()
    {
        if (Check(TsTokenType.Keyword, "if")) return ParseIfStmt();

        if (Check(TsTokenType.Keyword, "for")) return ParseForStmt();

        if (Check(TsTokenType.Keyword, "while")) return ParseWhileStmt();

        if (Check(TsTokenType.Keyword, "do")) return ParseDoWhileStmt();

        if (Check(TsTokenType.Keyword, "switch")) return ParseSwitchStmt();

        if (Check(TsTokenType.Keyword, "try")) return ParseTryStmt();

        if (Check(TsTokenType.Keyword, "return")) return ParseReturnStmt();

        if (Check(TsTokenType.Keyword, "throw")) return ParseThrowStmt();

        if (Check(TsTokenType.Keyword, "break")) return ParseBreakStmt();

        if (Check(TsTokenType.Keyword, "continue")) return ParseContinueStmt();

        if (Check(TsTokenType.Keyword, "debugger")) return ParseDebuggerStmt();

        if (Check(TsTokenType.Delimiter, "{")) return ParseBlockStmt();

        if (Check(TsTokenType.Delimiter, ";")) return ParseEmptyStmt();

        if (Check(TsTokenType.Identifier) && PeekNext().Type == TsTokenType.Punctuation &&
            PeekNext().Value == ":")
            return ParseLabeledStmt();

        return ParseExprStmt();
    }

    private TsBlockStmt ParseBlockStmt()
    {
        Consume(TsTokenType.Delimiter, "OAK2037", "期望 '{'");

        var statements = new List<TsAstNode>();

        while (!Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
        {
            var stmt = ParseDeclaration();

            if (stmt is not null) statements.Add(stmt);
        }

        Consume(TsTokenType.Delimiter, "OAK2038", "期望 '}'");

        return new TsBlockStmt(statements);
    }

    private TsIfStmt ParseIfStmt()
    {
        ConsumeKeyword("if", "OAK2039", "期望 'if' 关键字");
        Consume(TsTokenType.Delimiter, "OAK2040", "期望 '('");
        var condition = ParseExpression();
        Consume(TsTokenType.Delimiter, "OAK2041", "期望 ')'");
        var thenBlock = ParseStatement();

        TsAstNode? elseBlock = null;
        if (Match(TsTokenType.Keyword, "else")) elseBlock = ParseStatement();

        return new TsIfStmt(condition, thenBlock, elseBlock);
    }

    private TsAstNode ParseForStmt()
    {
        ConsumeKeyword("for", "OAK2042", "期望 'for' 关键字");

        var isAwait = Match(TsTokenType.Keyword, "await");

        Consume(TsTokenType.Delimiter, "OAK2043", "期望 '('");

        if (Check(TsTokenType.Keyword, "const") || Check(TsTokenType.Keyword, "let") ||
            Check(TsTokenType.Keyword, "var"))
        {
            var left = ParseVariableDecl();

            if (Check(TsTokenType.Keyword, "in"))
            {
                Advance();
                var right = ParseExpression();
                Consume(TsTokenType.Delimiter, "OAK2080", "期望 ')'");
                var body = ParseStatement();
                return new TsForInStmt(left, right, body);
            }

            if (Check(TsTokenType.Keyword, "of"))
            {
                Advance();
                var right = ParseExpression();
                Consume(TsTokenType.Delimiter, "OAK2081", "期望 ')'");
                var body = ParseStatement();
                return new TsForOfStmt(left, right, body, isAwait);
            }

            var init = left;

            TsAstNode? condition = null;
            if (!Check(TsTokenType.Delimiter, ";")) condition = ParseExpression();

            Match(TsTokenType.Delimiter, ";");

            TsAstNode? increment = null;
            if (!Check(TsTokenType.Delimiter, ")")) increment = ParseExpression();

            Consume(TsTokenType.Delimiter, "OAK2044", "期望 ')'");
            var forBody = ParseStatement();

            return new TsForStmt(init, condition, increment, forBody);
        }

        TsAstNode? forInit = null;
        if (!Check(TsTokenType.Delimiter, ";"))
        {
            forInit = ParseExpression();
            Match(TsTokenType.Delimiter, ";");
        }
        else
        {
            Match(TsTokenType.Delimiter, ";");
        }

        TsAstNode? forCondition = null;
        if (!Check(TsTokenType.Delimiter, ";")) forCondition = ParseExpression();

        Match(TsTokenType.Delimiter, ";");

        TsAstNode? forIncrement = null;
        if (!Check(TsTokenType.Delimiter, ")")) forIncrement = ParseExpression();

        Consume(TsTokenType.Delimiter, "OAK2044", "期望 ')'");
        var forBody2 = ParseStatement();

        return new TsForStmt(forInit, forCondition, forIncrement, forBody2);
    }

    private TsWhileStmt ParseWhileStmt()
    {
        ConsumeKeyword("while", "OAK2045", "期望 'while' 关键字");
        Consume(TsTokenType.Delimiter, "OAK2046", "期望 '('");
        var condition = ParseExpression();
        Consume(TsTokenType.Delimiter, "OAK2047", "期望 ')'");
        var body = ParseStatement();

        return new TsWhileStmt(condition, body);
    }

    private TsDoWhileStmt ParseDoWhileStmt()
    {
        ConsumeKeyword("do", "OAK2082", "期望 'do' 关键字");
        var body = ParseStatement();
        ConsumeKeyword("while", "OAK2083", "期望 'while' 关键字");
        Consume(TsTokenType.Delimiter, "OAK2084", "期望 '('");
        var condition = ParseExpression();
        Consume(TsTokenType.Delimiter, "OAK2085", "期望 ')'");
        Match(TsTokenType.Delimiter, ";");

        return new TsDoWhileStmt(body, condition);
    }

    private TsSwitchStmt ParseSwitchStmt()
    {
        ConsumeKeyword("switch", "OAK2086", "期望 'switch' 关键字");
        Consume(TsTokenType.Delimiter, "OAK2087", "期望 '('");
        var expression = ParseExpression();
        Consume(TsTokenType.Delimiter, "OAK2088", "期望 ')'");
        Consume(TsTokenType.Delimiter, "OAK2089", "期望 '{'");

        var cases = new List<TsSwitchCase>();

        while (!Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
        {
            if (Check(TsTokenType.Keyword, "case"))
            {
                Advance();
                var test = ParseExpression();
                Consume(TsTokenType.Punctuation, "OAK2090", "期望 ':'");

                var statements = new List<TsAstNode>();
                while (!Check(TsTokenType.Keyword, "case") && !Check(TsTokenType.Keyword, "default") &&
                       !Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
                {
                    var stmt = ParseDeclaration();
                    if (stmt is not null) statements.Add(stmt);
                }

                cases.Add(new TsSwitchCase(test, statements));
            }
            else if (Check(TsTokenType.Keyword, "default"))
            {
                Advance();
                Consume(TsTokenType.Punctuation, "OAK2091", "期望 ':'");

                var statements = new List<TsAstNode>();
                while (!Check(TsTokenType.Keyword, "case") && !Check(TsTokenType.Keyword, "default") &&
                       !Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
                {
                    var stmt = ParseDeclaration();
                    if (stmt is not null) statements.Add(stmt);
                }

                cases.Add(new TsSwitchCase(null, statements));
            }
            else
            {
                break;
            }
        }

        Consume(TsTokenType.Delimiter, "OAK2092", "期望 '}'");

        return new TsSwitchStmt(expression, cases);
    }

    private TsTryStmt ParseTryStmt()
    {
        ConsumeKeyword("try", "OAK2093", "期望 'try' 关键字");
        var block = ParseBlockStmt();

        TsCatchClause? catchClause = null;
        if (Match(TsTokenType.Keyword, "catch"))
        {
            string? paramName = null;
            TsAstNode? paramType = null;

            if (Match(TsTokenType.Delimiter, "("))
            {
                paramName = Consume(TsTokenType.Identifier, "OAK2094", "期望 catch 参数名").Value;

                if (Match(TsTokenType.Punctuation, ":")) paramType = ParseTypeAnnotation();

                Consume(TsTokenType.Delimiter, "OAK2095", "期望 ')'");
            }

            var catchBlock = ParseBlockStmt();
            catchClause = new TsCatchClause(paramName, paramType, catchBlock);
        }

        TsAstNode? finallyBlock = null;
        if (Match(TsTokenType.Keyword, "finally"))
        {
            finallyBlock = ParseBlockStmt();
        }

        return new TsTryStmt(block, catchClause, finallyBlock);
    }

    private TsReturnStmt ParseReturnStmt()
    {
        ConsumeKeyword("return", "OAK2048", "期望 'return' 关键字");

        TsAstNode? value = null;
        if (!Check(TsTokenType.Delimiter, ";") && !Check(TsTokenType.Delimiter, "}") &&
            !IsAtEnd())
            value = ParseExpression();

        Match(TsTokenType.Delimiter, ";");

        return new TsReturnStmt(value);
    }

    private TsThrowStmt ParseThrowStmt()
    {
        ConsumeKeyword("throw", "OAK2096", "期望 'throw' 关键字");
        var value = ParseExpression();
        Match(TsTokenType.Delimiter, ";");

        return new TsThrowStmt(value);
    }

    private TsBreakStmt ParseBreakStmt()
    {
        ConsumeKeyword("break", "OAK2097", "期望 'break' 关键字");

        string? label = null;
        if (Check(TsTokenType.Identifier) && !Check(TsTokenType.Delimiter, ";"))
            label = Advance().Value;

        Match(TsTokenType.Delimiter, ";");

        return new TsBreakStmt(label);
    }

    private TsContinueStmt ParseContinueStmt()
    {
        ConsumeKeyword("continue", "OAK2098", "期望 'continue' 关键字");

        string? label = null;
        if (Check(TsTokenType.Identifier) && !Check(TsTokenType.Delimiter, ";"))
            label = Advance().Value;

        Match(TsTokenType.Delimiter, ";");

        return new TsContinueStmt(label);
    }

    private TsDebuggerStmt ParseDebuggerStmt()
    {
        ConsumeKeyword("debugger", "OAK2099", "期望 'debugger' 关键字");
        Match(TsTokenType.Delimiter, ";");

        return new TsDebuggerStmt();
    }

    private TsEmptyStmt ParseEmptyStmt()
    {
        Consume(TsTokenType.Delimiter, "OAK2100", "期望 ';'");
        return new TsEmptyStmt();
    }

    private TsLabeledStmt ParseLabeledStmt()
    {
        var label = Consume(TsTokenType.Identifier, "OAK2101", "期望标签名").Value;
        Consume(TsTokenType.Punctuation, "OAK2102", "期望 ':'");
        var statement = ParseStatement();

        return new TsLabeledStmt(label, statement);
    }

    private TsExprStmt ParseExprStmt()
    {
        var expr = ParseExpression();
        Match(TsTokenType.Delimiter, ";");
        return new TsExprStmt(expr);
    }

    #endregion

    #region Expressions

    private TsAstNode ParseExpression()
    {
        return ParseAssignment();
    }

    private TsAstNode ParseAssignment()
    {
        var expr = ParseTernary();

        if (Check(TsTokenType.Operator, "=") || Check(TsTokenType.Operator, "+=") ||
            Check(TsTokenType.Operator, "-=") || Check(TsTokenType.Operator, "*=") ||
            Check(TsTokenType.Operator, "/=") || Check(TsTokenType.Operator, "%=") ||
            Check(TsTokenType.Operator, "&=") || Check(TsTokenType.Operator, "|=") ||
            Check(TsTokenType.Operator, "^=") || Check(TsTokenType.Operator, "<<=") ||
            Check(TsTokenType.Operator, ">>=") || Check(TsTokenType.Operator, ">>>="))
        {
            var op = Advance().Value;
            var right = ParseAssignment();
            return new TsAssignmentExpr(expr, op, right);
        }

        return expr;
    }

    private TsAstNode ParseTernary()
    {
        var expr = ParseOr();

        if (Match(TsTokenType.Operator, "?"))
        {
            var thenBranch = ParseExpression();
            Consume(TsTokenType.Punctuation, "OAK2049", "期望 ':'");
            var elseBranch = ParseExpression();
            return new TsConditionalExpr(expr, thenBranch, elseBranch);
        }

        return expr;
    }

    private TsAstNode ParseOr()
    {
        var left = ParseAnd();

        while (Match(TsTokenType.Operator, "||"))
        {
            var op = Previous().Value;
            var right = ParseAnd();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseAnd()
    {
        var left = ParseBitwiseOr();

        while (Match(TsTokenType.Operator, "&&"))
        {
            var op = Previous().Value;
            var right = ParseBitwiseOr();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseBitwiseOr()
    {
        var left = ParseBitwiseXor();

        while (Check(TsTokenType.Operator, "|") && !Check(TsTokenType.Operator, "||"))
        {
            var op = Advance().Value;
            var right = ParseBitwiseXor();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseBitwiseXor()
    {
        var left = ParseBitwiseAnd();

        while (Match(TsTokenType.Operator, "^"))
        {
            var op = Previous().Value;
            var right = ParseBitwiseAnd();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseBitwiseAnd()
    {
        var left = ParseEquality();

        while (Check(TsTokenType.Operator, "&") && !Check(TsTokenType.Operator, "&&"))
        {
            var op = Advance().Value;
            var right = ParseEquality();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseEquality()
    {
        var left = ParseRelational();

        while (Check(TsTokenType.Operator, "==") || Check(TsTokenType.Operator, "!=") ||
               Check(TsTokenType.Operator, "===") || Check(TsTokenType.Operator, "!=="))
        {
            var op = Advance().Value;
            var right = ParseRelational();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseRelational()
    {
        var left = ParseShift();

        while (Check(TsTokenType.Operator, "<") || Check(TsTokenType.Operator, ">") ||
               Check(TsTokenType.Operator, "<=") || Check(TsTokenType.Operator, ">=") ||
               Check(TsTokenType.Keyword, "as"))
        {
            var op = Advance().Value;
            var right = ParseShift();
            left = new TsBinaryExpr(left, op, right);
        }

        if (Check(TsTokenType.Keyword, "instanceof"))
        {
            Advance();
            var right = ParseShift();
            left = new TsInstanceofExpr(left, right);

            while (Check(TsTokenType.Operator, "<") || Check(TsTokenType.Operator, ">") ||
                   Check(TsTokenType.Operator, "<=") || Check(TsTokenType.Operator, ">=") ||
                   Check(TsTokenType.Keyword, "instanceof") || Check(TsTokenType.Keyword, "as"))
            {
                if (Check(TsTokenType.Keyword, "instanceof"))
                {
                    Advance();
                    right = ParseShift();
                    left = new TsInstanceofExpr(left, right);
                }
                else
                {
                    var op = Advance().Value;
                    var nextRight = ParseShift();
                    left = new TsBinaryExpr(left, op, nextRight);
                }
            }
        }

        if (Check(TsTokenType.Keyword, "in"))
        {
            Advance();
            var right = ParseShift();
            left = new TsBinaryExpr(left, "in", right);
        }

        return left;
    }

    private TsAstNode ParseShift()
    {
        var left = ParseAdditive();

        while (Check(TsTokenType.Operator, "<<") || Check(TsTokenType.Operator, ">>") ||
               Check(TsTokenType.Operator, ">>>"))
        {
            var op = Advance().Value;
            var right = ParseAdditive();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Check(TsTokenType.Operator, "+") || Check(TsTokenType.Operator, "-"))
        {
            var op = Advance().Value;
            var right = ParseMultiplicative();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseMultiplicative()
    {
        var left = ParseExponentiation();

        while (Check(TsTokenType.Operator, "*") || Check(TsTokenType.Operator, "/") ||
               Check(TsTokenType.Operator, "%"))
        {
            var op = Advance().Value;
            var right = ParseExponentiation();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseExponentiation()
    {
        var left = ParseUnary();

        if (Check(TsTokenType.Operator, "**"))
        {
            var op = Advance().Value;
            var right = ParseExponentiation();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode ParseUnary()
    {
        if (Check(TsTokenType.Operator, "-") || Check(TsTokenType.Operator, "+") ||
            Check(TsTokenType.Operator, "!") || Check(TsTokenType.Operator, "~") ||
            Check(TsTokenType.Operator, "++") || Check(TsTokenType.Operator, "--"))
        {
            var op = Advance().Value;
            var operand = ParseUnary();
            return new TsUnaryExpr(op, operand, true);
        }

        if (Check(TsTokenType.Keyword, "typeof"))
        {
            Advance();
            var operand = ParseUnary();
            return new TsTypeofExpr(operand);
        }

        if (Check(TsTokenType.Keyword, "void") || Check(TsTokenType.Keyword, "delete"))
        {
            var op = Advance().Value;
            var operand = ParseUnary();
            return new TsUnaryExpr(op, operand, true);
        }

        if (Check(TsTokenType.Keyword, "await"))
        {
            Advance();
            var operand = ParseUnary();
            return new TsUnaryExpr("await", operand, true);
        }

        if (Check(TsTokenType.Keyword, "yield")) return ParseYieldExpr();

        return ParsePostfix();
    }

    private TsYieldExpr ParseYieldExpr()
    {
        ConsumeKeyword("yield", "OAK2103", "期望 'yield' 关键字");

        var isDelegate = Match(TsTokenType.Operator, "*");

        TsAstNode? value = null;
        if (!Check(TsTokenType.Delimiter, ";") && !Check(TsTokenType.Delimiter, "}") && !IsAtEnd())
            value = ParseAssignment();

        return new TsYieldExpr(value, isDelegate);
    }

    private TsAstNode ParsePostfix()
    {
        var expr = ParseCallMember();

        if (Check(TsTokenType.Operator, "++") || Check(TsTokenType.Operator, "--"))
        {
            var op = Advance().Value;
            return new TsUnaryExpr(op, expr, false);
        }

        return expr;
    }

    private TsAstNode ParseCallMember()
    {
        var expr = ParsePrimary();

        while (true)
            if (Check(TsTokenType.Delimiter, "("))
            {
                expr = ParseCallExpr(expr);
            }
            else if (Match(TsTokenType.Delimiter, "."))
            {
                var memberName = Consume(TsTokenType.Identifier, "OAK2050", "期望成员名").Value;
                expr = new TsPropertyAccess(expr, memberName);
            }
            else if (Check(TsTokenType.Operator, "?."))
            {
                Advance();
                var memberName = Consume(TsTokenType.Identifier, "OAK2051", "期望成员名").Value;
                expr = new TsPropertyAccess(expr, memberName);
            }
            else if (Check(TsTokenType.Delimiter, "["))
            {
                Advance();
                var index = ParseExpression();
                Consume(TsTokenType.Delimiter, "OAK2052", "期望 ']'");
                expr = new TsElementAccess(expr, index);
            }
            else
            {
                break;
            }

        return expr;
    }

    private TsAstNode ParseCallExpr(TsAstNode callee)
    {
        Consume(TsTokenType.Delimiter, "OAK2053", "期望 '('");
        var args = new List<TsAstNode>();

        if (!Check(TsTokenType.Delimiter, ")"))
        {
            args.Add(ParseArgument());

            while (Match(TsTokenType.Delimiter, ",")) args.Add(ParseArgument());
        }

        Consume(TsTokenType.Delimiter, "OAK2054", "期望 ')'");
        return new TsCallExpr(callee, args);
    }

    private TsAstNode ParseArgument()
    {
        if (Match(TsTokenType.Operator, "...")) return new TsSpreadElement(ParseAssignment());

        return ParseAssignment();
    }

    private TsAstNode ParsePrimary()
    {
        if (Check(TsTokenType.Number))
        {
            Advance();
            return new TsLiteral("number", Previous().Value);
        }

        if (Check(TsTokenType.BigInt))
        {
            Advance();
            return new TsLiteral("bigint", Previous().Value);
        }

        if (Check(TsTokenType.String))
        {
            Advance();
            return new TsLiteral("string", Previous().Value);
        }

        if (Check(TsTokenType.TemplateString))
        {
            Advance();
            return new TsLiteral("template", Previous().Value);
        }

        if (Check(TsTokenType.Literal))
        {
            Advance();
            return new TsLiteral(Previous().Value, Previous().Value);
        }

        if (Check(TsTokenType.Keyword, "this"))
        {
            Advance();
            return new TsThisExpr();
        }

        if (Check(TsTokenType.Keyword, "super"))
        {
            Advance();
            return new TsSuperExpr();
        }

        if (Check(TsTokenType.Keyword, "function")) return ParseFunctionExpr();

        if (Check(TsTokenType.Keyword, "new")) return ParseNewExpr();

        if (Check(TsTokenType.Identifier))
        {
            var token = Advance();

            if (Check(TsTokenType.Operator, "=>"))
                return ParseArrowFunction([new TsParameter(Previous().Value, null, null)], false);

            return new TsIdentifier(Previous().Value);
        }

        if (Check(TsTokenType.Delimiter, "("))
        {
            return ParseParenOrArrowFunction();
        }

        if (Check(TsTokenType.Delimiter, "[")) return ParseArrayLiteral();

        if (Check(TsTokenType.Delimiter, "{")) return ParseObjectLiteral();

        var errorToken = Peek();
        _diagnostics?.AddError(
            _filePath,
            default,
            "OAK2060",
            $"意外的标记 '{errorToken.Value}'");

        throw new ParseException($"意外的标记 '{errorToken.Value}'");
    }

    private TsAstNode ParseParenOrArrowFunction()
    {
        var savedCurrent = _current;

        try
        {
            var parameters = TryParseArrowParameters();
            if (parameters is not null && Check(TsTokenType.Operator, "=>"))
            {
                return ParseArrowFunction(parameters, false);
            }
        }
        catch (ParseException)
        {
        }

        _current = savedCurrent;

        Advance();
        var expr = ParseExpression();
        Consume(TsTokenType.Delimiter, "OAK2059", "期望 ')'");

        if (Check(TsTokenType.Operator, "=>"))
            return ParseArrowFunction([new TsParameter("it", null, null)], false);

        return expr;
    }

    private IReadOnlyList<TsParameter>? TryParseArrowParameters()
    {
        if (!Check(TsTokenType.Delimiter, "(")) return null;

        Advance();

        var parameters = new List<TsParameter>();

        if (!Check(TsTokenType.Delimiter, ")"))
        {
            var param = TryParseSingleArrowParameter();
            if (param is null) return null;
            parameters.Add(param);

            while (Match(TsTokenType.Delimiter, ","))
            {
                param = TryParseSingleArrowParameter();
                if (param is null) return null;
                parameters.Add(param);
            }
        }

        if (!Check(TsTokenType.Delimiter, ")")) return null;
        Advance();

        return parameters;
    }

    private TsParameter? TryParseSingleArrowParameter()
    {
        if (Match(TsTokenType.Operator, "..."))
        {
            if (!Check(TsTokenType.Identifier)) return null;
            var name = Advance().Value;
            return new TsParameter($"...{name}", null, null);
        }

        if (!Check(TsTokenType.Identifier)) return null;
        var paramName = Advance().Value;

        TsAstNode? typeAnnotation = null;
        if (Check(TsTokenType.Punctuation, ":"))
        {
            Advance();
            typeAnnotation = ParseTypeAnnotation();
        }

        TsAstNode? defaultValue = null;
        if (Match(TsTokenType.Operator, "=")) defaultValue = ParseAssignment();

        return new TsParameter(paramName, typeAnnotation, defaultValue);
    }

    private TsNewExpr ParseNewExpr()
    {
        ConsumeKeyword("new", "OAK2055", "期望 'new' 关键字");
        var callee = ParseCallMember();

        if (callee is TsCallExpr callExpr) return new TsNewExpr(callExpr.Callee, callExpr.Arguments);

        return new TsNewExpr(callee, []);
    }

    private TsFunctionExpr ParseFunctionExpr()
    {
        ConsumeKeyword("function", "OAK2104", "期望 'function' 关键字");

        var isGenerator = Match(TsTokenType.Operator, "*");

        string? name = null;
        if (Check(TsTokenType.Identifier)) name = Advance().Value;

        Consume(TsTokenType.Delimiter, "OAK2105", "期望 '('");
        var parameters = ParseParameters();
        Consume(TsTokenType.Delimiter, "OAK2106", "期望 ')'");

        TsAstNode? returnType = null;
        if (Match(TsTokenType.Punctuation, ":")) returnType = ParseTypeAnnotation();

        var body = ParseBlockStmt();

        return new TsFunctionExpr(name, parameters, returnType, body, false, isGenerator);
    }

    private TsArrayLiteral ParseArrayLiteral()
    {
        Consume(TsTokenType.Delimiter, "OAK2061", "期望 '['");
        var elements = new List<TsAstNode>();

        if (!Check(TsTokenType.Delimiter, "]"))
        {
            elements.Add(ParseArrayElement());

            while (Match(TsTokenType.Delimiter, ","))
            {
                if (Check(TsTokenType.Delimiter, "]")) break;

                elements.Add(ParseArrayElement());
            }
        }

        Consume(TsTokenType.Delimiter, "OAK2062", "期望 ']'");
        return new TsArrayLiteral(elements);
    }

    private TsAstNode ParseArrayElement()
    {
        if (Match(TsTokenType.Operator, "...")) return new TsSpreadElement(ParseAssignment());

        return ParseAssignment();
    }

    private TsObjectLiteral ParseObjectLiteral()
    {
        Consume(TsTokenType.Delimiter, "OAK2063", "期望 '{'");
        var properties = new List<TsProperty>();

        if (!Check(TsTokenType.Delimiter, "}"))
        {
            properties.Add(ParseProperty());

            while (Match(TsTokenType.Delimiter, ","))
            {
                if (Check(TsTokenType.Delimiter, "}")) break;

                properties.Add(ParseProperty());
            }
        }

        Consume(TsTokenType.Delimiter, "OAK2064", "期望 '}'");
        return new TsObjectLiteral(properties);
    }

    private TsProperty ParseProperty()
    {
        var key = Consume(TsTokenType.Identifier, "OAK2065", "期望属性名").Value;
        Consume(TsTokenType.Punctuation, "OAK2066", "期望 ':'");
        var value = ParseAssignment();
        return new TsProperty(key, value);
    }

    private TsArrowFunctionExpr ParseArrowFunction(IReadOnlyList<TsParameter> parameters, bool isAsync)
    {
        Consume(TsTokenType.Operator, "OAK2067", "期望 '=>'");
        return ParseArrowFunctionBody(parameters, isAsync);
    }

    private TsArrowFunctionExpr ParseArrowFunctionBody(IReadOnlyList<TsParameter> parameters, bool isAsync)
    {
        TsAstNode? returnType = null;

        if (Match(TsTokenType.Punctuation, ":")) returnType = ParseTypeAnnotation();

        TsAstNode body;
        if (Check(TsTokenType.Delimiter, "{"))
            body = ParseBlockStmt();
        else
            body = ParseAssignment();

        return new TsArrowFunctionExpr(parameters, returnType, body, isAsync);
    }

    #endregion
}
