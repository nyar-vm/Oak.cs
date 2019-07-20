using System.Text;
using Oak.Diagnostics;
using Oak.Lexing;
using Oak.Syntax;
using Oak.Python.Syntax;

namespace Oak.Python.Lexer;

/// <summary>
///     Python 词法分析器，基于 Oak.Core 的 LexerBase 实现
/// </summary>
public sealed class PythonLexer : LexerBase
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "False", "None", "True", "and", "as", "assert", "async", "await",
        "break", "class", "continue", "def", "del", "elif", "else", "except",
        "finally", "for", "from", "global", "if", "import", "in", "is",
        "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try",
        "while", "with", "yield"
    };

    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        "+", "-", "*", "**", "/", "//", "%", "@",
        "<<", ">>", "&", "|", "^", "~",
        "<", ">", "<=", ">=", "==", "!=",
        "=", "+=", "-=", "*=", "/=", "//=", "%=", "@=",
        "&=", "|=", "^=", ">>=", "<<=", "**="
    };

    /// <summary>
    ///     创建 Python 词法分析器
    /// </summary>
    public PythonLexer(DiagnosticSink? diagnostics = null)
    {
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     将源代码转换为词法单元序列
    /// </summary>
    public override IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        Source = new StringSource(source);
        Reset();
        Diagnostics ??= new DiagnosticSink();

        var tokens = new List<GreenLeafNode>();
        var indentStack = new Stack<int>();
        indentStack.Push(0);

        var atLineStart = true;
        var pendingDedenTs = new List<GreenLeafNode>();

        while (!IsAtEnd())
        {
            if (atLineStart)
            {
                var indent = 0;
                while (!IsAtEnd() && (Peek() == ' ' || Peek() == '\t'))
                {
                    indent++;
                    Advance();
                }

                if (IsAtEnd() || Peek() == '\n' || Peek() == '\r')
                {
                    if (!IsAtEnd()) AdvanceNewLine();
                    continue;
                }

                if (Peek() == '#')
                {
                    SkipLineComment();
                    continue;
                }

                var currentIndent = indentStack.Peek();
                if (indent > currentIndent)
                {
                    indentStack.Push(indent);
                    tokens.Add(new GreenLeafNode(PythonNodeKind.Indent, string.Empty.Length, string.Empty));
                }
                else if (indent < currentIndent)
                {
                    while (indentStack.Count > 1 && indentStack.Peek() > indent)
                    {
                        indentStack.Pop();
                        pendingDedenTs.Add(new GreenLeafNode(PythonNodeKind.Dedent, string.Empty.Length, string.Empty));
                    }

                    if (indentStack.Peek() != indent)
                        Diagnostics.AddError(
                            string.Empty,
                            default,
                            "NPY1001",
                            "缩进不一致");
                }

                atLineStart = false;
            }

            foreach (var dedent in pendingDedenTs) tokens.Add(dedent);

            pendingDedenTs.Clear();

            if (IsAtEnd()) break;

            SkipWhitespace();

            if (IsAtEnd()) break;

            var c = Peek();

            if (c is '\n' or '\r')
            {
                AdvanceNewLine();
                tokens.Add(new GreenLeafNode(PythonNodeKind.NewLine, "\n".Length, "\n"));
                atLineStart = true;
                continue;
            }

            if (c == '#')
            {
                SkipLineComment();
                continue;
            }

            var token = ScanToken();
            if (token is not null) tokens.Add(token);
        }

        while (indentStack.Count > 1)
        {
            indentStack.Pop();
            tokens.Add(new GreenLeafNode(PythonNodeKind.Dedent, string.Empty.Length, string.Empty));
        }

        tokens.Add(new GreenLeafNode(PythonNodeKind.Eof, string.Empty.Length, string.Empty));
        return tokens;
    }

    /// <summary>
    ///     直接生成 GreenLeafNode 列表的词法分析方法
    /// </summary>
    public IReadOnlyList<GreenLeafNode> TokenizeToGreen(string source)
    {
        Source = new StringSource(source);
        Reset();
        Diagnostics ??= new DiagnosticSink();

        var greenNodes = new List<GreenLeafNode>();
        var indentStack = new Stack<int>();
        indentStack.Push(0);

        var atLineStart = true;
        var pendingDedents = new List<GreenLeafNode>();

        while (!IsAtEnd())
        {
            if (atLineStart)
            {
                var indent = 0;
                while (!IsAtEnd() && (Peek() == ' ' || Peek() == '\t'))
                {
                    indent++;
                    Advance();
                }

                if (IsAtEnd() || Peek() == '\n' || Peek() == '\r') continue;

                if (Peek() == '#')
                {
                    SkipLineComment();
                    continue;
                }

                var currentIndent = indentStack.Peek();
                if (indent > currentIndent)
                {
                    indentStack.Push(indent);
                    greenNodes.Add(new GreenLeafNode(PythonNodeKind.Indent, 0, string.Empty));
                }
                else if (indent < currentIndent)
                {
                    while (indentStack.Count > 1 && indentStack.Peek() > indent)
                    {
                        indentStack.Pop();
                        pendingDedents.Add(new GreenLeafNode(PythonNodeKind.Dedent, 0, string.Empty));
                    }

                    if (indentStack.Peek() != indent)
                        Diagnostics.AddError(
                            string.Empty,
                            default,
                            "NPY1001",
                            "缩进不一致");
                }

                atLineStart = false;
            }

            foreach (var dedent in pendingDedents) greenNodes.Add(dedent);

            pendingDedents.Clear();

            if (IsAtEnd()) break;

            SkipWhitespace();

            if (IsAtEnd()) break;

            var c = Peek();

            if (c is '\n' or '\r')
            {
                AdvanceNewLine();
                greenNodes.Add(new GreenLeafNode(PythonNodeKind.NewLine, 1, "\n"));
                atLineStart = true;
                continue;
            }

            if (c == '#')
            {
                SkipLineComment();
                continue;
            }

            var node = ScanGreenNode();
            if (node is not null) greenNodes.Add(node);
        }

        while (indentStack.Count > 1)
        {
            indentStack.Pop();
            greenNodes.Add(new GreenLeafNode(PythonNodeKind.Dedent, 0, string.Empty));
        }

        greenNodes.Add(new GreenLeafNode(PythonNodeKind.Eof, 0, string.Empty));
        return greenNodes;
    }

    /// <summary>
    ///     扫描单个 GreenLeafNode
    /// </summary>
    private GreenLeafNode? ScanGreenNode()
    {
        var c = Peek();

        if (c is '"' or '\'') return ScanStringGreen();

        if (char.IsDigit(c)) return ScanNumberGreen();

        if (c == '_' || char.IsLetter(c)) return ScanIdentifierOrKeywordGreen();

        if (IsOperatorStart(c)) return ScanOperatorGreen();

        if (IsDelimiter(c))
        {
            var value = c.ToString();
            Advance();
            return new GreenLeafNode(PythonNodeKind.Delimiter, value.Length, value);
        }

        Advance();
        Diagnostics.AddError(
            string.Empty,
            default,
            "NPY1002",
            $"意外的字符 '{c}'");

        return null;
    }

    /// <summary>
    ///     扫描字符串字面量为 GreenLeafNode
    /// </summary>
    private GreenLeafNode ScanStringGreen()
    {
        var quote = Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != quote)
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd()) break;

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }

        if (!IsAtEnd()) Advance();

        var value = sb.ToString();
        return new GreenLeafNode(PythonNodeKind.String, value.Length + 2, value);
    }

    /// <summary>
    ///     扫描数字字面量为 GreenLeafNode
    /// </summary>
    private GreenLeafNode ScanNumberGreen()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());

        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
        {
            sb.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            sb.Append(Advance());

            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) sb.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        }

        var value = sb.ToString();
        return new GreenLeafNode(PythonNodeKind.Number, value.Length, value);
    }

    /// <summary>
    ///     扫描标识符或关键字为 GreenLeafNode
    /// </summary>
    private GreenLeafNode ScanIdentifierOrKeywordGreen()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());

        var text = sb.ToString();
        var nodeKind = Keywords.Contains(text) ? PythonNodeKind.Keyword : PythonNodeKind.Identifier;

        return new GreenLeafNode(nodeKind, text.Length, text);
    }

    /// <summary>
    ///     扫描运算符为 GreenLeafNode
    /// </summary>
    private GreenLeafNode ScanOperatorGreen()
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd())
        {
            var candidate = sb.ToString() + Peek();

            if (Operators.Contains(candidate))
                sb.Append(Advance());
            else
                break;
        }

        var value = sb.ToString();
        return new GreenLeafNode(PythonNodeKind.Operator, value.Length, value);
    }

    private GreenLeafNode? ScanToken()
    {
        var c = Peek();

        if (c is '"' or '\'') return ScanString();

        if (char.IsDigit(c)) return ScanNumber();

        if (c == '_' || char.IsLetter(c)) return ScanIdentifierOrKeyword();

        if (IsOperatorStart(c)) return ScanOperator();

        if (IsDelimiter(c))
        {
            Advance();
            return new GreenLeafNode(PythonNodeKind.Delimiter, c.ToString().Length, c.ToString());
        }

        Advance();
        Diagnostics.AddError(
            string.Empty,
            default,
            "NPY1002",
            $"意外的字符 '{c}'");

        return null;
    }

    private GreenLeafNode ScanString()
    {
        var quote = Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != quote)
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd()) break;

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }

        if (!IsAtEnd()) Advance();

        return new GreenLeafNode(PythonNodeKind.String, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode ScanNumber()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());

        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
        {
            sb.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            sb.Append(Advance());

            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) sb.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        }

        return new GreenLeafNode(PythonNodeKind.Number, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode ScanIdentifierOrKeyword()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());

        var text = sb.ToString();

        if (Keywords.Contains(text)) return new GreenLeafNode(PythonNodeKind.Keyword, text.Length, text);

        return new GreenLeafNode(PythonNodeKind.Identifier, text.Length, text);
    }

    private GreenLeafNode ScanOperator()
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd())
        {
            var candidate = sb.ToString() + Peek();

            if (Operators.Contains(candidate))
                sb.Append(Advance());
            else
                break;
        }

        return new GreenLeafNode(PythonNodeKind.Operator, sb.ToString().Length, sb.ToString());
    }

    private static bool IsOperatorStart(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '@' or '&' or '|' or '^' or '~'
                or '<' or '>' or '=' or '!' => true,
            _ => false
        };
    }

    private static bool IsDelimiter(char c)
    {
        return c is '(' or ')' or '[' or ']' or '{' or '}' or ',' or ':' or ';' or '.';
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n') Advance();
    }

    private void AdvanceNewLine()
    {
        if (Peek() == '\r') Advance();

        if (Peek() == '\n') Advance();
    }

    /// <summary>
    ///     跳过空白字符（不包含换行符，换行符由缩进逻辑处理）
    /// </summary>
    private new void SkipWhitespace()
    {
        while (!IsAtEnd() && (Peek() == ' ' || Peek() == '\t')) Advance();
    }

    /// <summary>
    ///     执行词法分析，生成 GreenLeafNode 列表
    /// </summary>
    public IReadOnlyList<GreenLeafNode> TokenizeToGreen()
    {
        return TokenizeToGreen(Source.Substring(new Range(0, Source.Length)));
    }
}
