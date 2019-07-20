using Oak.Diagnostics;
using Oak.Parsing;
using Oak.Syntax;
using Oak.Python.AST;
using Oak.Python.Lexer;
using Oak.Python.Syntax;

namespace Oak.Python.Parser;

/// <summary>
///     Python 语法解析器，基于 Oak.Core 的 ParserBase 实现
/// </summary>
public sealed class PythonParser : ParserBase<IReadOnlyList<GreenLeafNode>, PyAstNode>
{
    private IReadOnlyList<GreenLeafNode> _tokens = [];
    private int _current;

    /// <summary>
    ///     创建 Python 语法解析器
    /// </summary>
    public PythonParser(DiagnosticSink? diagnostics = null)
        : base(diagnostics)
    {
    }

    /// <summary>
    ///     解析词法单元序列
    /// </summary>
    public override PyAstNode Parse(IReadOnlyList<GreenLeafNode> tokens)
    {
        _tokens = tokens;
        _current = 0;
        Diagnostics ??= new DiagnosticSink();

        var statements = new List<PyAstNode>();

        while (!IsAtEnd())
        {
            SkipNewLines();

            if (IsAtEnd())
            {
                break;
            }

            var stmt = ParseStatement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        return new PyModule(statements);
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().Kind == PythonNodeKind.Eof;
    }

    private GreenLeafNode Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private GreenLeafNode Previous()
    {
        return _tokens[_current - 1];
    }

    private GreenLeafNode Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }

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

    private GreenLeafNode Consume(NodeKind type, string errorCode, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        var token = Peek();
        Diagnostics.AddError(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private GreenLeafNode ConsumeKeyword(string keyword, string errorCode, string message)
    {
        if (Check(PythonNodeKind.Keyword, keyword))
        {
            return Advance();
        }

        var token = Peek();
        Diagnostics.AddError(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private void SkipNewLines()
    {
        while (Match(PythonNodeKind.NewLine))
        {
        }
    }

    private void ExpectIndent()
    {
        Consume(PythonNodeKind.Indent, "NPY2001", "期望缩进");
    }

    private void ExpectDedent()
    {
        Consume(PythonNodeKind.Dedent, "NPY2002", "期望反缩进");
    }

    #endregion

    #region Statements

    private PyAstNode? ParseStatement()
    {
        try
        {
            SkipNewLines();

            if (Check(PythonNodeKind.Keyword, "def"))
            {
                return ParseFunctionDef();
            }

            if (Check(PythonNodeKind.Keyword, "class"))
            {
                return ParseClassDef();
            }

            if (Check(PythonNodeKind.Keyword, "if"))
            {
                return ParseIfStmt();
            }

            if (Check(PythonNodeKind.Keyword, "while"))
            {
                return ParseWhileStmt();
            }

            if (Check(PythonNodeKind.Keyword, "for"))
            {
                return ParseForStmt();
            }

            if (Check(PythonNodeKind.Keyword, "return"))
            {
                return ParseReturnStmt();
            }

            if (Check(PythonNodeKind.Keyword, "yield"))
            {
                return ParseYieldStmt();
            }

            if (Check(PythonNodeKind.Keyword, "break"))
            {
                var token = Advance();
                return new PyBreak(Span: default);
            }

            if (Check(PythonNodeKind.Keyword, "continue"))
            {
                var token = Advance();
                return new PyContinue(Span: default);
            }

            if (Check(PythonNodeKind.Keyword, "pass"))
            {
                Advance();
                return new PyPass();
            }

            if (Check(PythonNodeKind.Keyword, "try"))
            {
                return ParseTryStmt();
            }

            if (Check(PythonNodeKind.Keyword, "raise"))
            {
                return ParseRaiseStmt();
            }

            if (Check(PythonNodeKind.Keyword, "import"))
            {
                return ParseImportStmt();
            }

            if (Check(PythonNodeKind.Keyword, "from"))
            {
                return ParseFromImportStmt();
            }

            return ParseExprStmt();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Kind == PythonNodeKind.NewLine)
            {
                return;
            }

            if (Check(PythonNodeKind.Keyword))
            {
                var value = Peek().Text;
                if (value is "def" or "class" or "if" or "while" or "for" or "return"
                    or "try" or "except" or "finally" or "raise" or "import" or "from"
                    or "break" or "continue" or "yield")
                {
                    return;
                }
            }

            Advance();
        }
    }

    private PyFunctionDef ParseFunctionDef()
    {
        var startToken = ConsumeKeyword("def", "NPY2003", "期望 'def' 关键字");

        var name = Consume(PythonNodeKind.Identifier, "NPY2004", "期望函数名").Text;

        Consume(PythonNodeKind.Delimiter, "NPY2005", "期望 '('");

        var parameters = new List<string>();

        if (!Check(PythonNodeKind.Delimiter, ")"))
        {
            parameters.Add(Consume(PythonNodeKind.Identifier, "NPY2006", "期望参数名").Text);

            while (Match(PythonNodeKind.Delimiter, ","))
            {
                parameters.Add(Consume(PythonNodeKind.Identifier, "NPY2007", "期望参数名").Text);
            }
        }

        Consume(PythonNodeKind.Delimiter, "NPY2008", "期望 ')'");
        Consume(PythonNodeKind.Delimiter, "NPY2009", "期望 ':'");

        var body = ParseSuite();

        return new PyFunctionDef(name, parameters, body, Span: default);
    }

    private PyClassDef ParseClassDef()
    {
        var startToken = ConsumeKeyword("class", "NPY2010", "期望 'class' 关键字");

        var name = Consume(PythonNodeKind.Identifier, "NPY2011", "期望类名").Text;

        Consume(PythonNodeKind.Delimiter, "NPY2012", "期望 ':'");

        var body = ParseSuite();

        return new PyClassDef(name, body, Span: default);
    }

    private PyIf ParseIfStmt()
    {
        var startToken = ConsumeKeyword("if", "NPY2013", "期望 'if' 关键字");

        var condition = ParseExpression();

        Consume(PythonNodeKind.Delimiter, "NPY2014", "期望 ':'");

        var thenBody = ParseSuite();

        List<PyAstNode>? elseBody = null;

        SkipNewLines();

        if (Match(PythonNodeKind.Keyword, "else"))
        {
            Consume(PythonNodeKind.Delimiter, "NPY2015", "期望 ':'");
            elseBody = ParseSuite().ToList();
        }
        else if (Check(PythonNodeKind.Keyword, "elif"))
        {
            elseBody = [ParseIfStmt()];
        }

        return new PyIf(condition, thenBody, elseBody, Span: default);
    }

    private PyWhile ParseWhileStmt()
    {
        var startToken = ConsumeKeyword("while", "NPY2016", "期望 'while' 关键字");

        var condition = ParseExpression();

        Consume(PythonNodeKind.Delimiter, "NPY2017", "期望 ':'");

        var body = ParseSuite();

        return new PyWhile(condition, body, Span: default);
    }

    private PyFor ParseForStmt()
    {
        var startToken = ConsumeKeyword("for", "NPY2018", "期望 'for' 关键字");

        var iterator = Consume(PythonNodeKind.Identifier, "NPY2019", "期望迭代变量").Text;

        ConsumeKeyword("in", "NPY2020", "期望 'in' 关键字");

        var iterable = ParseExpression();

        Consume(PythonNodeKind.Delimiter, "NPY2021", "期望 ':'");

        var body = ParseSuite();

        return new PyFor(iterator, iterable, body, Span: default);
    }

    private PyReturn ParseReturnStmt()
    {
        var startToken = ConsumeKeyword("return", "NPY2022", "期望 'return' 关键字");

        PyAstNode? value = null;

        if (!Check(PythonNodeKind.NewLine) && !Check(PythonNodeKind.Dedent) && !IsAtEnd())
        {
            value = ParseExpression();
        }

        return new PyReturn(value, Span: default);
    }

    private PyAstNode ParseExprStmt()
    {
        var expr = ParseExpression();

        if (Match(PythonNodeKind.Operator, "="))
        {
            var value = ParseExpression();
            return new PyAssign(expr, value);
        }

        var augAssignOps = new[] { "+=", "-=", "*=", "/=", "//=", "%=", "@=", "&=", "|=", "^=", ">>=", "<<=", "**=" };
        foreach (var augOp in augAssignOps)
        {
            if (Match(PythonNodeKind.Operator, augOp))
            {
                var value = ParseExpression();
                return new PyAugAssign(expr, augOp.TrimEnd('='), value);
            }
        }

        return new PyExprStmt(expr);
    }

    private PyYield ParseYieldStmt()
    {
        var startToken = ConsumeKeyword("yield", "NPY2023", "期望 'yield' 关键字");

        PyAstNode? value = null;

        if (!Check(PythonNodeKind.NewLine) && !Check(PythonNodeKind.Dedent) && !IsAtEnd())
        {
            value = ParseExpression();
        }

        return new PyYield(value, Span: default);
    }

    private PyTry ParseTryStmt()
    {
        var startToken = ConsumeKeyword("try", "NPY2024", "期望 'try' 关键字");

        Consume(PythonNodeKind.Delimiter, "NPY2025", "期望 ':'");

        var body = ParseSuite();

        var handlers = new List<PyExceptClause>();

        while (Match(PythonNodeKind.Keyword, "except"))
        {
            PyAstNode? exceptionType = null;
            string? exceptName = null;

            if (!Check(PythonNodeKind.Delimiter, ":"))
            {
                exceptionType = ParseExpression();

                if (Match(PythonNodeKind.Keyword, "as"))
                {
                    exceptName = Consume(PythonNodeKind.Identifier, "NPY2026", "期望异常变量名").Text;
                }
            }

            Consume(PythonNodeKind.Delimiter, "NPY2027", "期望 ':'");
            var handlerBody = ParseSuite();

            handlers.Add(new PyExceptClause(exceptionType, exceptName, handlerBody));
        }

        IReadOnlyList<PyAstNode>? elseBody = null;

        SkipNewLines();

        if (Match(PythonNodeKind.Keyword, "else"))
        {
            Consume(PythonNodeKind.Delimiter, "NPY2028", "期望 ':'");
            elseBody = ParseSuite().ToList();
        }

        IReadOnlyList<PyAstNode>? finallyBody = null;

        SkipNewLines();

        if (Match(PythonNodeKind.Keyword, "finally"))
        {
            Consume(PythonNodeKind.Delimiter, "NPY2029", "期望 ':'");
            finallyBody = ParseSuite().ToList();
        }

        return new PyTry(body, handlers, elseBody, finallyBody, Span: default);
    }

    private PyRaise ParseRaiseStmt()
    {
        var startToken = ConsumeKeyword("raise", "NPY2030", "期望 'raise' 关键字");

        PyAstNode? exception = null;

        if (!Check(PythonNodeKind.NewLine) && !Check(PythonNodeKind.Dedent) && !IsAtEnd())
        {
            exception = ParseExpression();
        }

        return new PyRaise(exception, Span: default);
    }

    private PyImport ParseImportStmt()
    {
        var startToken = ConsumeKeyword("import", "NPY2031", "期望 'import' 关键字");

        var items = new List<PyImportItem> { ParseImportItem() };

        while (Match(PythonNodeKind.Delimiter, ","))
        {
            items.Add(ParseImportItem());
        }

        return new PyImport(items, Span: default);
    }

    private PyFromImport ParseFromImportStmt()
    {
        var startToken = ConsumeKeyword("from", "NPY2032", "期望 'from' 关键字");

        var moduleBuilder = new System.Text.StringBuilder();
        moduleBuilder.Append(Consume(PythonNodeKind.Identifier, "NPY2033", "期望模块名").Text);

        while (Match(PythonNodeKind.Delimiter, "."))
        {
            moduleBuilder.Append('.');

            if (Check(PythonNodeKind.Identifier))
            {
                moduleBuilder.Append(Advance().Text);
            }
        }

        var module = moduleBuilder.ToString();

        ConsumeKeyword("import", "NPY2034", "期望 'import' 关键字");

        var items = new List<PyImportItem> { ParseImportItem() };

        while (Match(PythonNodeKind.Delimiter, ","))
        {
            items.Add(ParseImportItem());
        }

        return new PyFromImport(module, items, Span: default);
    }

    private PyImportItem ParseImportItem()
    {
        var name = Consume(PythonNodeKind.Identifier, "NPY2036", "期望标识符").Text;

        string? alias = null;

        if (Match(PythonNodeKind.Keyword, "as"))
        {
            alias = Consume(PythonNodeKind.Identifier, "NPY2037", "期望别名").Text;
        }

        return new PyImportItem(name, alias);
    }

    private IReadOnlyList<PyAstNode> ParseSuite()
    {
        var statements = new List<PyAstNode>();

        if (Check(PythonNodeKind.NewLine))
        {
            Advance();
            ExpectIndent();

            while (!Check(PythonNodeKind.Dedent) && !IsAtEnd())
            {
                SkipNewLines();

                if (Check(PythonNodeKind.Dedent) || IsAtEnd())
                {
                    break;
                }

                var stmt = ParseStatement();
                if (stmt is not null)
                {
                    statements.Add(stmt);
                }
            }

            ExpectDedent();
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        return statements;
    }

    #endregion

    #region Expressions

    private PyAstNode ParseExpression()
    {
        return ParseOr();
    }

    private PyAstNode ParseOr()
    {
        var left = ParseAnd();

        while (Match(PythonNodeKind.Keyword, "or"))
        {
            var op = Previous().Text;
            var right = ParseAnd();
            left = new PyBinaryOp(left, op, right);
        }

        return left;
    }

    private PyAstNode ParseAnd()
    {
        var left = ParseNot();

        while (Match(PythonNodeKind.Keyword, "and"))
        {
            var op = Previous().Text;
            var right = ParseNot();
            left = new PyBinaryOp(left, op, right);
        }

        return left;
    }

    private PyAstNode ParseNot()
    {
        if (Match(PythonNodeKind.Keyword, "not"))
        {
            var operand = ParseNot();
            return new PyUnaryOp("not", operand);
        }

        return ParseComparison();
    }

    private PyAstNode ParseComparison()
    {
        var left = ParseAddition();

        while (Check(PythonNodeKind.Operator) &&
               (Peek().Text is "==" or "!=" or "<" or ">" or "<=" or ">="))
        {
            var op = Advance().Text;
            var right = ParseAddition();
            left = new PyBinaryOp(left, op, right);
        }

        return left;
    }

    private PyAstNode ParseAddition()
    {
        var left = ParseMultiplication();

        while (Check(PythonNodeKind.Operator) && (Peek().Text is "+" or "-"))
        {
            var op = Advance().Text;
            var right = ParseMultiplication();
            left = new PyBinaryOp(left, op, right);
        }

        return left;
    }

    private PyAstNode ParseMultiplication()
    {
        var left = ParseUnary();

        while (Check(PythonNodeKind.Operator) &&
               (Peek().Text is "*" or "/" or "//" or "%" or "@"))
        {
            var op = Advance().Text;
            var right = ParseUnary();
            left = new PyBinaryOp(left, op, right);
        }

        return left;
    }

    private PyAstNode ParseUnary()
    {
        if (Check(PythonNodeKind.Operator) && (Peek().Text is "+" or "-" or "~"))
        {
            var op = Advance().Text;
            var operand = ParseUnary();
            return new PyUnaryOp(op, operand);
        }

        return ParsePower();
    }

    private PyAstNode ParsePower()
    {
        var left = ParsePostfix();

        if (Match(PythonNodeKind.Operator, "**"))
        {
            var right = ParseUnary();
            left = new PyBinaryOp(left, "**", right);
        }

        return left;
    }

    private PyAstNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Match(PythonNodeKind.Delimiter, "."))
            {
                var name = Consume(PythonNodeKind.Identifier, "NPY2030", "期望属性名").Text;
                expr = new PyAttribute(expr, name);
            }
            else if (Match(PythonNodeKind.Delimiter, "("))
            {
                var args = new List<PyAstNode>();

                if (!Check(PythonNodeKind.Delimiter, ")"))
                {
                    args.Add(ParseExpression());

                    while (Match(PythonNodeKind.Delimiter, ","))
                    {
                        args.Add(ParseExpression());
                    }
                }

                Consume(PythonNodeKind.Delimiter, "NPY2031", "期望 ')'");
                expr = new PyCall(expr, args);
            }
            else if (Match(PythonNodeKind.Delimiter, "["))
            {
                var index = ParseExpression();
                Consume(PythonNodeKind.Delimiter, "NPY2032", "期望 ']'");
                expr = new PySubscript(expr, index);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private PyAstNode ParsePrimary()
    {
        if (Match(PythonNodeKind.Number))
        {
            var token = Previous();
            return new PyLiteral("number", Peek().Text, Span: default);
        }

        if (Match(PythonNodeKind.String))
        {
            var token = Previous();
            return new PyLiteral("string", Peek().Text, Span: default);
        }

        if (Check(PythonNodeKind.Keyword))
        {
            var token = Peek();
            if (Peek().Text is "True" or "False" or "None")
            {
                Advance();
                return new PyLiteral(Peek().Text.ToLowerInvariant(), Peek().Text, Span: default);
            }
        }

        if (Match(PythonNodeKind.Identifier))
        {
            var token = Previous();
            return new PyIdentifier(Peek().Text, Span: default);
        }

        if (Match(PythonNodeKind.Delimiter, "("))
        {
            var expr = ParseExpression();
            Consume(PythonNodeKind.Delimiter, "NPY2033", "期望 ')'");
            return expr;
        }

        if (Match(PythonNodeKind.Delimiter, "["))
        {
            var elements = new List<PyAstNode>();

            if (!Check(PythonNodeKind.Delimiter, "]"))
            {
                elements.Add(ParseExpression());

                while (Match(PythonNodeKind.Delimiter, ","))
                {
                    elements.Add(ParseExpression());
                }
            }

            Consume(PythonNodeKind.Delimiter, "NPY2034", "期望 ']'");
            return new PyList(elements);
        }

        var errorToken = Peek();
        Diagnostics.AddError(
            string.Empty,
            default,
            "NPY2035",
            $"意外的标记 '{errorToken.Text}'");

        throw new ParseException($"意外的标记 '{errorToken.Text}'");
    }

    #endregion
}
