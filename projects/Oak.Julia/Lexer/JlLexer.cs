using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Julia.Lexer;

public sealed class JlLexer
{
    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        "+", "-", "*", "/", "//", "^", "%", "\\", "~",
        "==", "!=", "===", "!==", "<", ">", "<=", ">=",
        "=", "+=", "-=", "*=", "/=", "//=", "^=", "%=", "\\=",
        "&&", "||", "!", "<<<", ">>>", "<<", ">>",
        "&", "|",
        "->", "=>", ":", "::", "?", "...", "..",
        ".", "|>", "<|"
    };

    private static readonly HashSet<char> Delimiters = ['(', ')', '{', '}', '[', ']', ',', ';'];

    private DiagnosticSink? _diagnostics;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private ISource _source = StringSource.Empty;

    public JlLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public IReadOnlyList<JlToken> Tokenize(string source)
    {
        _source = new StringSource(source);
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<JlToken>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();
            if (IsAtEnd()) break;
            var token = ScanToken();
            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new JlToken(JlTokenType.Eof, string.Empty, _line, _column));
        return tokens;
    }

    private bool IsAtEnd() => _position >= _source.Length;
    private char Peek() => IsAtEnd() ? '\0' : _source[_position];
    private char PeekNext() => Peek(1);
    private char Peek(int offset) { var i = _position + offset; return i >= _source.Length ? '\0' : _source[i]; }

    private char Advance()
    {
        var c = _source[_position];
        _position++;
        if (c == '\n') { _line++; _column = 1; } else { _column++; }
        return c;
    }

    private JlToken? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;
        var c = Peek();

        if (c == '"') { if (PeekNext() == '"' && Peek(2) == '"') return ScanTripleQuotedString(startLine, startColumn); return ScanString(startLine, startColumn); }
        if (c == '\'') return ScanChar(startLine, startColumn);
        if (c == '`') { if (PeekNext() == '`' && Peek(2) == '`') return ScanTripleCommand(startLine, startColumn); return ScanCommand(startLine, startColumn); }
        if (char.IsDigit(c)) return ScanNumber(startLine, startColumn);
        if (c == '@') return ScanMacroName(startLine, startColumn);
        if (c == '_' || char.IsLetter(c) || c > 127) return ScanIdentifierOrKeyword(startLine, startColumn);
        if (IsOperatorStart(c)) return ScanOperator(startLine, startColumn);
        if (Delimiters.Contains(c)) { Advance(); return new JlToken(JlTokenType.Delimiter, c.ToString(), startLine, startColumn); }

        Advance();
        _diagnostics?.AddError(string.Empty, default, "JLA1001", $"意外的字符 '{c}'");
        return null;
    }

    private JlToken ScanString(int startLine, int startColumn)
    {
        Advance();
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\') { Advance(); if (IsAtEnd()) break; var e = Advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '"' => '"', '\'' => '\'', '0' => '\0', _ => e }); }
            else sb.Append(Advance());
        }
        if (IsAtEnd()) _diagnostics?.AddError(string.Empty, default, "JLA1002", "未闭合的字符串字面量");
        else Advance();
        return new JlToken(JlTokenType.String, sb.ToString(), startLine, startColumn);
    }

    private JlToken ScanTripleQuotedString(int startLine, int startColumn)
    {
        Advance(); Advance(); Advance();
        var sb = new StringBuilder();
        while (!IsAtEnd())
        {
            if (Peek() == '"' && PeekNext() == '"' && Peek(2) == '"') { Advance(); Advance(); Advance(); break; }
            if (Peek() == '\\') { Advance(); if (!IsAtEnd()) { var e = Advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '"' => '"', _ => e }); } }
            else sb.Append(Advance());
        }
        return new JlToken(JlTokenType.String, sb.ToString(), startLine, startColumn);
    }

    private JlToken ScanChar(int startLine, int startColumn)
    {
        Advance();
        var sb = new StringBuilder();
        if (!IsAtEnd() && Peek() == '\\') { Advance(); if (!IsAtEnd()) { var e = Advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '\'' => '\'', '0' => '\0', _ => e }); } }
        else if (!IsAtEnd()) sb.Append(Advance());
        if (!IsAtEnd() && Peek() == '\'') Advance();
        else _diagnostics?.AddError(string.Empty, default, "JLA1003", "未闭合的字符字面量");
        return new JlToken(JlTokenType.Char, sb.ToString(), startLine, startColumn);
    }

    private JlToken ScanCommand(int startLine, int startColumn)
    {
        Advance();
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '`') sb.Append(Advance());
        if (!IsAtEnd()) Advance();
        return new JlToken(JlTokenType.CommandType, sb.ToString(), startLine, startColumn);
    }

    private JlToken ScanTripleCommand(int startLine, int startColumn)
    {
        Advance(); Advance(); Advance();
        var sb = new StringBuilder();
        while (!IsAtEnd()) { if (Peek() == '`' && PeekNext() == '`' && Peek(2) == '`') { Advance(); Advance(); Advance(); break; } sb.Append(Advance()); }
        return new JlToken(JlTokenType.CommandType, sb.ToString(), startLine, startColumn);
    }

    private JlToken ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        if (Peek() == '0' && (PeekNext() == 'x' || PeekNext() == 'X')) { Advance(); Advance(); sb.Append("0x"); while (!IsAtEnd() && IsHexDigit(Peek())) sb.Append(Advance()); return new JlToken(JlTokenType.Number, sb.ToString(), startLine, startColumn); }
        if (Peek() == '0' && (PeekNext() == 'b' || PeekNext() == 'B')) { Advance(); Advance(); sb.Append("0b"); while (!IsAtEnd() && IsBinaryDigit(Peek())) sb.Append(Advance()); return new JlToken(JlTokenType.Number, sb.ToString(), startLine, startColumn); }
        if (Peek() == '0' && (PeekNext() == 'o' || PeekNext() == 'O')) { Advance(); Advance(); sb.Append("0o"); while (!IsAtEnd() && IsOctalDigit(Peek())) sb.Append(Advance()); return new JlToken(JlTokenType.Number, sb.ToString(), startLine, startColumn); }
        while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext())) { sb.Append(Advance()); while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance()); }
        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E')) { sb.Append(Advance()); if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) sb.Append(Advance()); while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance()); }
        if (!IsAtEnd() && (Peek() == 'i' || Peek() == 'f')) sb.Append(Advance());
        return new JlToken(JlTokenType.Number, sb.ToString(), startLine, startColumn);
    }

    private static bool IsHexDigit(char c) => char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
    private static bool IsOctalDigit(char c) => c is >= '0' and <= '7';
    private static bool IsBinaryDigit(char c) => c is '0' or '1';

    private JlToken ScanMacroName(int startLine, int startColumn)
    {
        Advance();
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || Peek() == '!' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
        return new JlToken(JlTokenType.MacroName, sb.ToString(), startLine, startColumn);
    }

    private JlToken ScanIdentifierOrKeyword(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || Peek() == '!' || char.IsLetterOrDigit(Peek()) || Peek() > 127)) sb.Append(Advance());
        if (!IsAtEnd() && Peek() == '!' && sb.Length > 0) sb.Append(Advance());
        var text = sb.ToString();
        if (JlKeywords.All.Contains(text)) return new JlToken(JlTokenType.Keyword, text, startLine, startColumn);
        return new JlToken(JlTokenType.Identifier, text, startLine, startColumn);
    }

    private JlToken ScanOperator(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());
        while (!IsAtEnd()) { var candidate = sb.ToString() + Peek(); if (Operators.Contains(candidate)) sb.Append(Advance()); else break; }
        var op = sb.ToString();
        if (op == ":") return new JlToken(JlTokenType.Punctuation, op, startLine, startColumn);
        return new JlToken(JlTokenType.Operator, op, startLine, startColumn);
    }

    private static bool IsOperatorStart(char c) => c is '+' or '-' or '*' or '/' or '%' or '^' or '\\' or '=' or '!' or '<' or '>' or '&' or '|' or '~' or '?' or ':' or '.';

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            switch (Peek())
            {
                case ' ' or '\t' or '\r' or '\n': Advance(); break;
                case '#': if (PeekNext() == '=') SkipBlockComment(); else SkipLineComment(); break;
                default: return;
            }
        }
    }

    private void SkipLineComment() { while (!IsAtEnd() && Peek() != '\n') Advance(); }

    private void SkipBlockComment()
    {
        Advance(); Advance();
        var startLine = _line; var startColumn = _column; var depth = 1;
        while (!IsAtEnd() && depth > 0)
        {
            if (Peek() == '#' && PeekNext() == '=') { Advance(); Advance(); depth++; }
            else if (Peek() == '=' && PeekNext() == '#') { Advance(); Advance(); depth--; }
            else Advance();
        }
        if (depth > 0) _diagnostics?.AddError(string.Empty, default, "JLA1004", "未闭合的块注释");
    }
}
