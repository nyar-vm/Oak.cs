using System.Text;
using Oak.Diagnostics;
using Oak.Lexing;
using Oak.Syntax;

namespace Oak.C;

/// <summary>
///     C 语言词法分析器
/// </summary>
public sealed class CLexer : LexerBase
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "auto", "break", "case", "char", "const", "continue", "default", "do",
        "double", "else", "enum", "extern", "float", "for", "goto", "if",
        "inline", "int", "long", "register", "restrict", "return", "short", "signed",
        "sizeof", "static", "struct", "switch", "typedef", "union", "unsigned", "void",
        "volatile", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex",
        "_Generic", "_Imaginary", "_Noreturn", "_Static_assert", "_Thread_local"
    };

    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        "+", "-", "*", "/", "%", "++", "--",
        "==", "!=", ">", "<", ">=", "<=",
        "&&", "||", "!",
        "&", "|", "^", "~", "<<", ">>",
        "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=",
        ".", "->", "...",
        "?", ":"
    };

    public CLexer(DiagnosticSink? diagnostics = null)
    {
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     将源代码转换为词法单元序列（GreenLeafNode）
    /// </summary>
    public override IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        var ctokens = TokenizeAsCTokens(source);
        return ctokens.Select(t => new GreenLeafNode(t.Kind, t.Text.Length, t.Text)).ToList();
    }

    /// <summary>
    ///     将源代码转换为 C 词法单元序列
    /// </summary>
    public IReadOnlyList<CToken> TokenizeAsCTokens(string source)
    {
        Source = new StringSource(source);
        Reset();
        Diagnostics ??= new DiagnosticSink();

        var tokens = new List<CToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();

            if (IsAtEnd()) break;

            var c = Peek();

            if (c is '\n' or '\r')
            {
                AdvanceNewLine();
                tokens.Add(new CToken(CNodeKind.NewLine, "\n"));
                continue;
            }

            if (c == '#')
            {
                var pp = ScanPreprocessorText();
                if (pp is not null) tokens.Add(new CToken(CNodeKind.Preprocessor, pp));
                continue;
            }

            if (c == '/' && PeekNext() == '/')
            {
                SkipLineComment();
                continue;
            }

            if (c == '/' && PeekNext() == '*')
            {
                var comment = ScanBlockCommentText();
                if (comment is not null) tokens.Add(new CToken(CNodeKind.Comment, comment));
                continue;
            }

            var (kind, text) = ScanTokenParts();

            if (kind is not null)
                tokens.Add(new CToken(kind.Value, text));
        }

        tokens.Add(new CToken(CNodeKind.Eof, ""));
        return tokens;
    }

    private (NodeKind? Kind, string Text) ScanTokenParts()
    {
        var c = Peek();

        if (c is '"') return (CNodeKind.String, ScanStringText());
        if (c is '\'') return (CNodeKind.Char, ScanCharText());
        if (char.IsDigit(c)) return (CNodeKind.Number, ScanNumberText());
        if (c == '_' || char.IsLetter(c)) return ScanIdentifierOrKeywordText();
        if (IsOperatorStart(c)) return (CNodeKind.Operator, ScanOperatorText());
        if (IsDelimiter(c)) { Advance(); return (CNodeKind.Delimiter, c.ToString()); }
        Advance();
        Diagnostics?.AddError(
            string.Empty,
            default,
            "OC1001",
            $"意外的字符 '{c}'");

        return (null, "");
    }

    private string ScanStringText()
    {
        Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd()) break;
                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '"' => '"', '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }

        if (!IsAtEnd()) Advance();
        return sb.ToString();
    }

    private string ScanCharText()
    {
        Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '\'')
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd()) break;
                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '\'' => '\'', '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }

        if (!IsAtEnd()) Advance();
        return sb.ToString();
    }

    private string ScanNumberText()
    {
        var sb = new StringBuilder();
        var isHex = false;
        var isBinary = false;

        if (Peek() == '0')
        {
            sb.Append(Advance());
            if (!IsAtEnd() && (Peek() == 'x' || Peek() == 'X')) { sb.Append(Advance()); isHex = true; }
            else if (!IsAtEnd() && (Peek() == 'b' || Peek() == 'B')) { sb.Append(Advance()); isBinary = true; }
        }

        if (isHex) while (!IsAtEnd() && (char.IsDigit(Peek()) || (Peek() >= 'a' && Peek() <= 'f') || (Peek() >= 'A' && Peek() <= 'F'))) sb.Append(Advance());
        else if (isBinary) while (!IsAtEnd() && (Peek() == '0' || Peek() == '1')) sb.Append(Advance());
        else while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());

        if (!isHex && !isBinary && !IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
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

        while (!IsAtEnd() && (Peek() == 'u' || Peek() == 'U' || Peek() == 'l' || Peek() == 'L' || Peek() == 'f' || Peek() == 'F')) sb.Append(Advance());

        return sb.ToString();
    }

    private (NodeKind Kind, string Text) ScanIdentifierOrKeywordText()
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
        var text = sb.ToString();
        return Keywords.Contains(text) ? (CNodeKind.Keyword, text) : (CNodeKind.Identifier, text);
    }

    private string ScanOperatorText()
    {
        var sb = new StringBuilder();
        sb.Append(Advance());
        while (!IsAtEnd())
        {
            var candidate = sb.ToString() + Peek();
            if (Operators.Contains(candidate)) sb.Append(Advance());
            else break;
        }
        return sb.ToString();
    }

    private string? ScanPreprocessorText()
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '\n') sb.Append(Advance());
        var value = sb.ToString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n') Advance();
    }

    private string? ScanBlockCommentText()
    {
        var sb = new StringBuilder();
        sb.Append(Advance());
        sb.Append(Advance());
        while (!IsAtEnd() && !(Peek() == '*' && PeekNext() == '/')) sb.Append(Advance());
        if (!IsAtEnd()) { sb.Append(Advance()); sb.Append(Advance()); }
        var value = sb.ToString();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool IsOperatorStart(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '&' or '|' or '^' or '~'
                or '<' or '>' or '=' or '!' or '.' or '?' or ':' => true,
            _ => false
        };
    }

    private static bool IsDelimiter(char c)
    {
        return c is '(' or ')' or '[' or ']' or '{' or '}' or ',' or ';';
    }

    private void AdvanceNewLine()
    {
        if (Peek() == '\r') Advance();
        if (Peek() == '\n') Advance();
    }

    private new void SkipWhitespace()
    {
        while (!IsAtEnd() && (Peek() == ' ' || Peek() == '\t' || Peek() == '\f' || Peek() == '\v')) Advance();
    }
}
