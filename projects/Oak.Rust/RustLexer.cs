using System.Text;
using Oak.Diagnostics;
using Oak.Lexing;
using Oak.Syntax;

namespace Oak.Rust;

/// <summary>
///     Rust 语言词法分析器
/// </summary>
public sealed class RustLexer : LexerBase
{
    private int _line = 1;
    private int _column = 1;

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "as", "async", "await", "break", "const", "continue", "crate", "dyn",
        "else", "enum", "extern", "fn", "for", "if", "impl", "in",
        "let", "loop", "match", "mod", "move", "mut", "pub", "ref",
        "return", "self", "Self", "static", "struct", "super", "trait", "type",
        "unsafe", "use", "where", "while", "yield",
        "true", "false"
    };

    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        "+", "-", "*", "/", "%",
        "==", "!=", ">", "<", ">=", "<=",
        "&&", "||", "!",
        "&", "|", "^", "~", "<<", ">>",
        "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=",
        ".", "..", "...", "..=",
        "->", "=>", "?", ":", "::", ";",
        "@", "#", "$"
    };

    public RustLexer(DiagnosticSink? diagnostics = null)
    {
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     将源代码转换为词法单元序列
    /// </summary>
    public override IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        var tokens = TokenizeAsRustTokens(source);
        var nodes = new List<GreenLeafNode>();

        foreach (var token in tokens)
        {
            nodes.Add(new GreenLeafNode(token.Kind, token.Text.Length, token.Text));
        }

        return nodes;
    }

    /// <summary>
    ///     将源代码转换为 RustToken 序列
    /// </summary>
    public IReadOnlyList<RustToken> TokenizeAsRustTokens(string source)
    {
        Source = new StringSource(source);
        Reset();
        Diagnostics ??= new DiagnosticSink();

        var tokens = new List<RustToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();

            if (IsAtEnd()) break;

            var c = Peek();
            var line = _line;
            var column = _column;

            if (c is '\n' or '\r')
            {
                AdvanceNewLine();
                tokens.Add(new RustToken(RustNodeKind.NewLine, "\n", line, column));
                continue;
            }

            if (c == '/' && PeekNext() == '/')
            {
                SkipLineComment();
                continue;
            }

            if (c == '/' && PeekNext() == '*')
            {
                ScanBlockCommentToken(tokens, line, column);
                continue;
            }

            ScanToken(tokens, line, column);
        }

        tokens.Add(new RustToken(RustNodeKind.Eof, string.Empty, _line, _column));
        return tokens;
    }

    private void ScanToken(List<RustToken> tokens, int line, int column)
    {
        var c = Peek();

        if (c == '"')
        {
            ScanStringToken(tokens, line, column);
            return;
        }

        if (c == '\'')
        {
            ScanCharOrLifetimeToken(tokens, line, column);
            return;
        }

        if (char.IsDigit(c))
        {
            ScanNumberToken(tokens, line, column);
            return;
        }

        if (c == '_' || char.IsLetter(c))
        {
            ScanIdentifierOrKeywordToken(tokens, line, column);
            return;
        }

        if (IsOperatorStart(c))
        {
            ScanOperatorToken(tokens, line, column);
            return;
        }

        if (IsDelimiter(c))
        {
            Advance();
            tokens.Add(new RustToken(RustNodeKind.Delimiter, c.ToString(), line, column));
            return;
        }

        Advance();
        Diagnostics?.AddError(
            string.Empty,
            default,
            "OR1001",
            $"意外的字符 '{c}'");
    }

    private void ScanStringToken(List<RustToken> tokens, int line, int column)
    {
        Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
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
                    '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd()) Advance();

        tokens.Add(new RustToken(RustNodeKind.String, sb.ToString(), line, column));
    }

    private void ScanCharOrLifetimeToken(List<RustToken> tokens, int line, int column)
    {
        Advance();

        if (!IsAtEnd() && Peek() != '\'' && !char.IsWhiteSpace(Peek()) && Peek() != '\\')
        {
            var next = Advance();

            if (!IsAtEnd() && Peek() == '\'')
            {
                Advance();
                tokens.Add(new RustToken(RustNodeKind.Char, next.ToString(), line, column));
                return;
            }

            var sb = new StringBuilder();
            sb.Append(next);

            while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek())))
            {
                sb.Append(Advance());
            }

            tokens.Add(new RustToken(RustNodeKind.Identifier, $"'{sb}", line, column));
            return;
        }

        tokens.Add(new RustToken(RustNodeKind.Identifier, "'", line, column));
    }

    private void ScanNumberToken(List<RustToken> tokens, int line, int column)
    {
        var sb = new StringBuilder();
        var isHex = false;
        var isBinary = false;
        var isOctal = false;

        if (Peek() == '0')
        {
            sb.Append(Advance());

            if (!IsAtEnd())
            {
                if (Peek() == 'x' || Peek() == 'X')
                {
                    sb.Append(Advance());
                    isHex = true;
                }
                else if (Peek() == 'b' || Peek() == 'B')
                {
                    sb.Append(Advance());
                    isBinary = true;
                }
                else if (Peek() == 'o' || Peek() == 'O')
                {
                    sb.Append(Advance());
                    isOctal = true;
                }
            }
        }

        if (isHex)
        {
            while (!IsAtEnd() && (char.IsDigit(Peek()) || (Peek() >= 'a' && Peek() <= 'f') ||
                                  (Peek() >= 'A' && Peek() <= 'F') || Peek() == '_'))
            {
                var c = Advance();
                if (c != '_') sb.Append(c);
            }
        }
        else if (isBinary)
        {
            while (!IsAtEnd() && (Peek() == '0' || Peek() == '1' || Peek() == '_'))
            {
                var c = Advance();
                if (c != '_') sb.Append(c);
            }
        }
        else if (isOctal)
        {
            while (!IsAtEnd() && ((Peek() >= '0' && Peek() <= '7') || Peek() == '_'))
            {
                var c = Advance();
                if (c != '_') sb.Append(c);
            }
        }
        else
        {
            while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == '_'))
            {
                var c = Advance();
                if (c != '_') sb.Append(c);
            }
        }

        if (!isHex && !isBinary && !isOctal && !IsAtEnd() && Peek() == '.' && !IsDelimiter(PeekNext()))
        {
            sb.Append(Advance());
            while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == '_'))
            {
                var c = Advance();
                if (c != '_') sb.Append(c);
            }
        }

        if (!isHex && !isBinary && !isOctal && !IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            sb.Append(Advance());
            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) sb.Append(Advance());
            while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == '_'))
            {
                var c = Advance();
                if (c != '_') sb.Append(c);
            }
        }

        while (!IsAtEnd() && (Peek() == 'f' || Peek() == 'F' || Peek() == 'i' || Peek() == 'I' || Peek() == 'u' ||
                              Peek() == 'U'))
        {
            sb.Append(Advance());

            if (!IsAtEnd() && char.IsDigit(Peek()))
            {
                while (!IsAtEnd() && char.IsDigit(Peek()))
                {
                    sb.Append(Advance());
                }
            }

            break;
        }

        tokens.Add(new RustToken(RustNodeKind.Number, sb.ToString(), line, column));
    }

    private void ScanIdentifierOrKeywordToken(List<RustToken> tokens, int line, int column)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek())))
        {
            sb.Append(Advance());
        }

        var text = sb.ToString();
        var kind = Keywords.Contains(text) ? RustNodeKind.Keyword : RustNodeKind.Identifier;
        tokens.Add(new RustToken(kind, text, line, column));
    }

    private void ScanOperatorToken(List<RustToken> tokens, int line, int column)
    {
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

        tokens.Add(new RustToken(RustNodeKind.Operator, sb.ToString(), line, column));
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n') Advance();
    }

    private void ScanBlockCommentToken(List<RustToken> tokens, int line, int column)
    {
        var sb = new StringBuilder();

        sb.Append(Advance());
        sb.Append(Advance());

        var depth = 1;

        while (!IsAtEnd() && depth > 0)
        {
            if (Peek() == '/' && PeekNext() == '*')
            {
                sb.Append(Advance());
                sb.Append(Advance());
                depth++;
            }
            else if (Peek() == '*' && PeekNext() == '/')
            {
                sb.Append(Advance());
                sb.Append(Advance());
                depth--;
            }
            else
            {
                sb.Append(Advance());
            }
        }

        var value = sb.ToString();
        if (!string.IsNullOrEmpty(value))
        {
            tokens.Add(new RustToken(RustNodeKind.Comment, value, line, column));
        }
    }

    private static bool IsOperatorStart(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '&' or '|' or '^' or '~'
                or '<' or '>' or '=' or '!' or '.' or '?' or ':' or '@' or '#' or '$' => true,
            _ => false
        };
    }

    private static bool IsDelimiter(char c)
    {
        return c is '(' or ')' or '[' or ']' or '{' or '}' or ',' or ';';
    }

    private void AdvanceNewLine()
    {
        if (Peek() == '\r')
        {
            Advance();
            _column = 1;
        }

        if (Peek() == '\n')
        {
            Advance();
            _line++;
            _column = 1;
        }
    }

    private new void SkipWhitespace()
    {
        while (!IsAtEnd())
        {
            var c = Peek();
            if (c is ' ' or '\t' or '\f' or '\v')
            {
                Advance();
                _column++;
            }
            else if (c is '\n' or '\r')
            {
                AdvanceNewLine();
            }
            else
            {
                break;
            }
        }
    }
}
