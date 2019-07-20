using System.Text;
using Oak.Diagnostics;
using Oak.Lexing;
using Oak.Syntax;

namespace Oak.Widget.Lexer;

/// <summary>
///     AWSL 词法分析器，将 AWSL 模板源码转换为 GreenLeafNode 序列。
///     支持模板标签、表达式插值、脚本声明、样式定义等 AWSL 语法。
/// </summary>
public sealed class AwslLexer : LexerBase
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "let", "const", "micro",
        "if", "else", "for", "foreach", "in",
        "return", "break", "continue",
        "import", "export", "using", "namespace",
        "widget", "component", "plugin",
        "match", "case", "end",
        "struct", "class", "enums", "flags", "union",
        "type"
    };

    private static readonly HashSet<string> Literals = new(StringComparer.Ordinal)
    {
        "true", "false", "null"
    };

    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        "+", "-", "*", "/", "%",
        "==", "!=", "<", ">", "<=", ">=",
        "&&", "||", "!",
        "&", "|", "^", "~",
        "=", "+=", "-=", "*=", "/=", "%=",
        "<<", ">>", "<<=", ">>=",
        "=>", "->", "??",
        "++", "--",
        "?.",
        "</", "/>"
    };

    private static readonly HashSet<char> Delimiters = ['(', ')', '[', ']', '{', '}', ','];

    public AwslLexer(DiagnosticSink? diagnostics = null)
    {
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     将 AWSL 源码转换为词法单元序列
    /// </summary>
    /// <param name="source">AWSL 源码文本</param>
    /// <returns>词法单元列表</returns>
    public override IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        Source = new StringSource(source);
        Reset();
        Diagnostics ??= new DiagnosticSink();

        var nodes = new List<GreenLeafNode>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();

            if (IsAtEnd())
            {
                break;
            }

            var node = ScanNode();

            if (node is not null)
            {
                nodes.Add(node);
            }
        }

        nodes.Add(new GreenLeafNode(AwslNodeKind.Eof, 0, string.Empty));
        return nodes;
    }

    #region 主扫描方法

    private GreenLeafNode? ScanNode()
    {
        var c = Peek();

        if (c == '@')
        {
            return ScanAtPrefixed();
        }

        if (c is '"' or '\'' or '`')
        {
            return ScanString();
        }

        if (char.IsDigit(c))
        {
            return ScanNumber();
        }

        if (c == '_' || char.IsLetter(c))
        {
            return ScanIdentifierOrKeyword();
        }

        if (c == '.')
        {
            var next = PeekNext();

            if (char.IsDigit(next))
            {
                return ScanNumber();
            }

            return ScanOperatorOrDelimiter();
        }

        if (c is ':' or ';')
        {
            return ScanPunctuation();
        }

        if (IsOperatorStart(c))
        {
            return ScanOperatorOrDelimiter();
        }

        if (Delimiters.Contains(c))
        {
            Advance();
            return new GreenLeafNode(AwslNodeKind.Delimiter, 1, c.ToString());
        }

        Advance();
        Diagnostics?.AddWarning(
            string.Empty,
            default,
            "AWSL1001",
            $"意外的字符 '{c}'");

        return null;
    }

    #endregion

    #region 注释跳过

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            var c = Peek();

            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    Advance();
                    break;
                case '<':
                    if (Peek(1) == '!' && Peek(2) == '-' && Peek(3) == '-')
                    {
                        SkipHtmlComment();
                    }
                    else
                    {
                        return;
                    }

                    break;
                case '/':
                    if (PeekNext() == '/')
                    {
                        SkipLineComment();
                    }
                    else if (PeekNext() == '*')
                    {
                        SkipBlockComment();
                    }
                    else
                    {
                        return;
                    }

                    break;
                default:
                    return;
            }
        }
    }

    private void SkipHtmlComment()
    {
        Advance();
        Advance();
        Advance();
        Advance();

        while (!IsAtEnd())
        {
            if (Peek() == '-' && Peek(1) == '-' && Peek(2) == '>')
            {
                Advance();
                Advance();
                Advance();
                return;
            }

            Advance();
        }

        Diagnostics?.AddWarning(
            string.Empty,
            default,
            "AWSL1002",
            "未闭合的 HTML 注释");
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n')
        {
            Advance();
        }
    }

    private void SkipBlockComment()
    {
        Advance();
        Advance();

        var depth = 1;

        while (!IsAtEnd() && depth > 0)
        {
            if (Peek() == '/' && PeekNext() == '*')
            {
                Advance();
                Advance();
                depth++;
            }
            else if (Peek() == '*' && PeekNext() == '/')
            {
                Advance();
                Advance();
                depth--;
            }
            else
            {
                Advance();
            }
        }

        if (depth > 0)
        {
            Diagnostics?.AddWarning(
                string.Empty,
                default,
                "AWSL1003",
                "未闭合的块注释");
        }
    }

    #endregion

    #region 字面量扫描

    private GreenLeafNode ScanNumber()
    {
        var sb = new StringBuilder();

        if (Peek() == '0' && (PeekNext() == 'x' || PeekNext() == 'X'))
        {
            sb.Append(Advance());
            sb.Append(Advance());

            while (!IsAtEnd() && IsHexDigit(Peek()))
            {
                sb.Append(Advance());
            }

            return new GreenLeafNode(AwslNodeKind.Number, sb.Length, sb.ToString());
        }

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
        {
            sb.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd() && (Peek() == 'f' || Peek() == 'F'))
        {
            sb.Append(Advance());
        }

        var value = sb.ToString();
        return new GreenLeafNode(AwslNodeKind.Number, value.Length, value);
    }

    private GreenLeafNode ScanString()
    {
        var quote = Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != quote)
        {
            if (Peek() == '\\')
            {
                Advance();

                if (IsAtEnd())
                {
                    break;
                }

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    '`' => '`',
                    '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
        {
            Diagnostics?.AddError(
                string.Empty,
                default,
                "AWSL1004",
                "未闭合的字符串字面量");
        }
        else
        {
            Advance();
        }

        var value = sb.ToString();
        return new GreenLeafNode(AwslNodeKind.String, value.Length, value);
    }

    #endregion

    #region 标识符与关键字扫描

    private GreenLeafNode ScanIdentifierOrKeyword()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && (Peek() == '_' || Peek() == '-' || char.IsLetterOrDigit(Peek())))
        {
            sb.Append(Advance());
        }

        var text = sb.ToString();

        if (Literals.Contains(text))
        {
            return new GreenLeafNode(AwslNodeKind.Literal, text.Length, text);
        }

        if (Keywords.Contains(text))
        {
            return new GreenLeafNode(AwslNodeKind.Keyword, text.Length, text);
        }

        return new GreenLeafNode(AwslNodeKind.Identifier, text.Length, text);
    }

    #endregion

    #region 运算符与标点扫描

    private GreenLeafNode ScanPunctuation()
    {
        var c = Advance();

        if (c == ':' && Peek() == ':')
        {
            Advance();
            return new GreenLeafNode(AwslNodeKind.Punctuation, 2, "::");
        }

        return new GreenLeafNode(AwslNodeKind.Punctuation, 1, c.ToString());
    }

    private static bool IsOperatorStart(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '=' or '!' or '<' or '>' or '&' or '|' or '^' or '~' or '?' => true,
            _ => false
        };
    }

    private GreenLeafNode ScanOperatorOrDelimiter()
    {
        if (Peek() == '.')
        {
            Advance();
            return new GreenLeafNode(AwslNodeKind.Operator, 1, ".");
        }

        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd())
        {
            var candidate = sb.ToString() + Peek();

            if (Operators.Contains(candidate))
            {
                sb.Append(Advance());
            }
            else
            {
                break;
            }
        }

        var op = sb.ToString();

        if (op is ":" or "::" or ";" or ",")
        {
            return new GreenLeafNode(AwslNodeKind.Punctuation, op.Length, op);
        }

        if (op is "/")
        {
            // 检查 / 后面是否是 >（自闭合标签 / >），这里分开处理为两个 operator
            // 单字 / 是除法运算符
            return new GreenLeafNode(AwslNodeKind.Operator, op.Length, op);
        }

        return new GreenLeafNode(AwslNodeKind.Operator, op.Length, op);
    }

    #endregion

    #region @ 前缀扫描

    /// <summary>
    ///     扫描 @click, @bind, @input 等事件绑定或响应式绑定前缀
    /// </summary>
    private GreenLeafNode ScanAtPrefixed()
    {
        Advance();

        if (IsAtEnd())
        {
            return new GreenLeafNode(AwslNodeKind.Operator, 1, "@");
        }

        var c = Peek();

        if (c == '_' || char.IsLetter(c))
        {
            var sb = new StringBuilder();

            while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek())))
            {
                sb.Append(Advance());
            }

            var name = sb.ToString();
            return new GreenLeafNode(AwslNodeKind.AtPrefix, name.Length + 1, name);
        }

        return new GreenLeafNode(AwslNodeKind.Operator, 1, "@");
    }

    #endregion

    #region 辅助方法

    private static bool IsHexDigit(char c)
    {
        return char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
    }

    #endregion
}
