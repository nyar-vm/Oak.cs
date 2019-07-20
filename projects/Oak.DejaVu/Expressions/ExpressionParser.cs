using Oak.Diagnostics;
using Oak.Parsing;

namespace Oak.DejaVu.Expressions;

/// <summary>
///     表达式解析器，使用 Pratt Parser 算法
/// </summary>
public sealed class ExpressionParser
{
    private readonly DiagnosticSink _diagnostics;

    /// <summary>
    ///     创建表达式解析器
    /// </summary>
    /// <param name="diagnostics">诊断消息收集器</param>
    public ExpressionParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    /// <summary>
    ///     解析表达式
    /// </summary>
    public IExpressionNode Parse(string expression)
    {
        var lexer = new ExpressionLexer(expression);
        var tokens = lexer.Tokenize();
        var reader = new TokenReader(tokens);
        return ParseExpression(reader, 0);
    }

    /// <summary>
    ///     解析表达式（Pratt Parser 核心算法）
    /// </summary>
    private IExpressionNode ParseExpression(TokenReader reader, int precedence)
    {
        if (reader.IsAtEnd)
        {
            _diagnostics.AddError("", default, "EmptyExpression", "Empty expression");
            return new LiteralNode { Value = null };
        }

        var token = reader.Advance();
        var left = ParsePrefix(token, reader);

        while (!reader.IsAtEnd && precedence < GetPrecedence(reader.Current.Type))
        {
            token = reader.Advance();
            left = ParseInfix(token, left, reader);
        }

        return left;
    }

    /// <summary>
    ///     解析前缀表达式
    /// </summary>
    private IExpressionNode ParsePrefix(ExpressionToken token, TokenReader reader)
    {
        return token.Type switch
        {
            ExpressionTokenType.Number => new LiteralNode { Value = token.Value },
            ExpressionTokenType.String => new LiteralNode { Value = token.Value },
            ExpressionTokenType.Boolean => new LiteralNode { Value = token.Value },
            ExpressionTokenType.Identifier => ParseIdentifier(token, reader),
            ExpressionTokenType.Minus => new UnaryNode
            {
                Operator = UnaryOperator.Negate,
                Operand = ParseExpression(reader, GetPrecedence(ExpressionTokenType.Minus))
            },
            ExpressionTokenType.Not => new UnaryNode
            {
                Operator = UnaryOperator.Not,
                Operand = ParseExpression(reader, GetPrecedence(ExpressionTokenType.Not))
            },
            ExpressionTokenType.LeftParen => ParseGroup(reader),
            _ => throw new ParseException($"Unexpected token: {token.Type}")
        };
    }

    /// <summary>
    ///     解析中缀表达式
    /// </summary>
    private IExpressionNode ParseInfix(ExpressionToken token, IExpressionNode left, TokenReader reader)
    {
        return token.Type switch
        {
            ExpressionTokenType.Plus => new BinaryNode
            {
                Operator = BinaryOperator.Add,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.Minus => new BinaryNode
            {
                Operator = BinaryOperator.Subtract,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.Multiply => new BinaryNode
            {
                Operator = BinaryOperator.Multiply,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.Divide => new BinaryNode
            {
                Operator = BinaryOperator.Divide,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.Modulo => new BinaryNode
            {
                Operator = BinaryOperator.Modulo,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.Equal => new BinaryNode
            {
                Operator = BinaryOperator.Equal,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.NotEqual => new BinaryNode
            {
                Operator = BinaryOperator.NotEqual,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.LessThan => new BinaryNode
            {
                Operator = BinaryOperator.LessThan,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.LessThanOrEqual => new BinaryNode
            {
                Operator = BinaryOperator.LessThanOrEqual,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.GreaterThan => new BinaryNode
            {
                Operator = BinaryOperator.GreaterThan,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.GreaterThanOrEqual => new BinaryNode
            {
                Operator = BinaryOperator.GreaterThanOrEqual,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.And => new BinaryNode
            {
                Operator = BinaryOperator.And,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.Or => new BinaryNode
            {
                Operator = BinaryOperator.Or,
                Left = left,
                Right = ParseExpression(reader, GetPrecedence(token.Type))
            },
            ExpressionTokenType.Pipe => ParsePipe(left, reader),
            ExpressionTokenType.Dot => ParseMemberAccess(left, reader),
            ExpressionTokenType.LeftParen => ParseCall(left, reader),
            ExpressionTokenType.LeftBracket => ParseIndex(left, reader),
            _ => throw new ParseException($"Unexpected token: {token.Type}")
        };
    }

    /// <summary>
    ///     解析标识符
    /// </summary>
    private IExpressionNode ParseIdentifier(ExpressionToken token, TokenReader reader)
    {
        if (reader is { IsAtEnd: false, Current.Type: ExpressionTokenType.LeftParen })
            return ParseCall(new IdentifierNode { Name = token.Value?.ToString() ?? "" }, reader);
        return new IdentifierNode { Name = token.Value?.ToString() ?? "" };
    }

    /// <summary>
    ///     解析分组表达式
    /// </summary>
    private IExpressionNode ParseGroup(TokenReader reader)
    {
        var expr = ParseExpression(reader, 0);
        if (reader.IsAtEnd || reader.Current.Type != ExpressionTokenType.RightParen)
            _diagnostics.AddError("", default, "MissingClosingParen", "Missing closing parenthesis");
        else
            reader.Advance();
        return expr;
    }

    /// <summary>
    ///     解析成员访问
    /// </summary>
    private IExpressionNode ParseMemberAccess(IExpressionNode left, TokenReader reader)
    {
        if (reader.IsAtEnd || reader.Current.Type != ExpressionTokenType.Identifier)
        {
            _diagnostics.AddError("", default, "MissingMemberName", "Missing member name after dot");
            return left;
        }

        var memberName = reader.Advance().Value?.ToString() ?? "";
        return new MemberAccessNode { Object = left, MemberName = memberName };
    }

    /// <summary>
    ///     解析函数调用
    /// </summary>
    private IExpressionNode ParseCall(IExpressionNode left, TokenReader reader)
    {
        var arguments = new List<IExpressionNode>();
        reader.Advance(); // 跳过左括号

        while (!reader.IsAtEnd && reader.Current.Type != ExpressionTokenType.RightParen)
        {
            arguments.Add(ParseExpression(reader, 0));
            if (reader is { IsAtEnd: false, Current.Type: ExpressionTokenType.Comma }) reader.Advance();
        }

        if (reader.IsAtEnd || reader.Current.Type != ExpressionTokenType.RightParen)
            _diagnostics.AddError("", default, "MissingClosingParen", "Missing closing parenthesis in function call");
        else
            reader.Advance();

        return new CallNode { Function = left, Arguments = arguments };
    }

    /// <summary>
    ///     解析索引访问
    /// </summary>
    private IExpressionNode ParseIndex(IExpressionNode left, TokenReader reader)
    {
        reader.Advance(); // 跳过左括号
        var index = ParseExpression(reader, 0);

        if (reader.IsAtEnd || reader.Current.Type != ExpressionTokenType.RightBracket)
            _diagnostics.AddError("", default, "MissingClosingBracket", "Missing closing bracket");
        else
            reader.Advance();

        return new IndexNode { Object = left, Index = index };
    }

    /// <summary>
    ///     解析管道表达式
    /// </summary>
    private IExpressionNode ParsePipe(IExpressionNode left, TokenReader reader)
    {
        if (reader.IsAtEnd || reader.Current.Type != ExpressionTokenType.Identifier)
        {
            _diagnostics.AddError("", default, "MissingFilterName", "Missing filter name after |>");
            return left;
        }

        var filterName = reader.Advance().Value?.ToString() ?? "";
        var arguments = new List<IExpressionNode>();

        if (reader is { IsAtEnd: false, Current.Type: ExpressionTokenType.Colon })
        {
            reader.Advance();
            while (!reader.IsAtEnd && reader.Current.Type != ExpressionTokenType.Pipe)
            {
                arguments.Add(ParseExpression(reader, 0));
                if (reader is { IsAtEnd: false, Current.Type: ExpressionTokenType.Comma })
                {
                    reader.Advance();
                }
            }
        }

        var pipeNode = new PipeNode { Left = left, FilterName = filterName, Arguments = arguments };
        return pipeNode;
    }

    /// <summary>
    ///     获取运算符优先级
    /// </summary>
    private int GetPrecedence(ExpressionTokenType tokenType)
    {
        return tokenType switch
        {
            ExpressionTokenType.Pipe => 5,
            ExpressionTokenType.Or => 10,
            ExpressionTokenType.And => 20,
            ExpressionTokenType.Equal => 30,
            ExpressionTokenType.NotEqual => 30,
            ExpressionTokenType.LessThan => 40,
            ExpressionTokenType.LessThanOrEqual => 40,
            ExpressionTokenType.GreaterThan => 40,
            ExpressionTokenType.GreaterThanOrEqual => 40,
            ExpressionTokenType.Plus => 50,
            ExpressionTokenType.Minus => 50,
            ExpressionTokenType.Multiply => 60,
            ExpressionTokenType.Divide => 60,
            ExpressionTokenType.Modulo => 60,
            ExpressionTokenType.Dot => 70,
            ExpressionTokenType.LeftParen => 80,
            ExpressionTokenType.LeftBracket => 80,
            _ => 0
        };
    }
}