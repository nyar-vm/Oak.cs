using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Jasmin;

/// <summary>
///     Jasmin 词法分析器
///     将 Jasmin 源码文本分解为 Token 流
/// </summary>
public sealed class JmLexer
{
    private string _source = "";
    private int _position;
    private int _line;
    private int _column;
    private DiagnosticSink? _diagnostics;

    private static readonly HashSet<string> Directives =
    [
        ".class", ".super", ".implements", ".interface", ".field",
        ".method", ".end", ".limit", ".line", ".var", ".throws",
        ".catch", ".source", ".version", ".attribute", ".debug"
    ];

    private static readonly HashSet<string> AccessModifiers =
    [
        "public", "private", "protected", "static", "final",
        "synchronized", "volatile", "transient", "native",
        "abstract", "strictfp", "enum", "annotation", "interface"
    ];

    /// <summary>
    ///     词法分析
    /// </summary>
    /// <param name="source">Jasmin 源码文本</param>
    /// <param name="diagnostics">诊断接收器</param>
    /// <returns>Token 列表</returns>
    public IReadOnlyList<JmToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<JmToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();
            if (IsAtEnd()) break;

            var token = ScanToken();
            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new JmToken(JmTokenType.Eof, "", _line, _column));
        return tokens;
    }

    /// <summary>
    ///     扫描下一个 Token
    /// </summary>
    private JmToken? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;

        if (Peek() == ';')
        {
            return ScanComment(startLine, startColumn);
        }

        if (Peek() == '"')
        {
            return ScanString(startLine, startColumn);
        }

        if (Peek() == ':')
        {
            Advance();
            return new JmToken(JmTokenType.Colon, ":", startLine, startColumn);
        }

        if (Peek() == '=')
        {
            Advance();
            return new JmToken(JmTokenType.Equals, "=", startLine, startColumn);
        }

        if (Peek() == '.')
        {
            return ScanDirective(startLine, startColumn);
        }

        if (char.IsDigit(Peek()) || (Peek() == '-' && Peek(1) is not '\0' && char.IsDigit(Peek(1))))
        {
            return ScanNumber(startLine, startColumn);
        }

        if (IsIdentifierStart(Peek()))
        {
            return ScanIdentifierOrOpcode(startLine, startColumn);
        }

        _diagnostics?.AddWarning("", default, "JM1001", $"未识别的字符 '{Peek()}'");
        Advance();
        return null;
    }

    /// <summary>
    ///     扫描注释
    /// </summary>
    private JmToken ScanComment(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '\n')
        {
            sb.Append(Advance());
        }

        return new JmToken(JmTokenType.Comment, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描字符串字面量
    /// </summary>
    private JmToken ScanString(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\')
            {
                sb.Append(Advance());
                if (!IsAtEnd()) sb.Append(Advance());
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd()) sb.Append(Advance());

        return new JmToken(JmTokenType.StringLiteral, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描指令关键字
    /// </summary>
    private JmToken ScanDirective(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
        {
            sb.Append(Advance());
        }

        return new JmToken(JmTokenType.Directive, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描数字
    /// </summary>
    private JmToken ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (Peek() == '-')
        {
            sb.Append(Advance());
        }

        while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == 'x' || IsHexDigit(Peek())))
        {
            sb.Append(Advance());
        }

        return new JmToken(JmTokenType.Number, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描标识符或操作码
    /// </summary>
    private JmToken ScanIdentifierOrOpcode(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            sb.Append(Advance());
        }

        var value = sb.ToString();

        if (value.EndsWith(':'))
        {
            return new JmToken(JmTokenType.Label, value[..^1], startLine, startColumn);
        }

        if (AccessModifiers.Contains(value))
        {
            return new JmToken(JmTokenType.AccessModifier, value, startLine, startColumn);
        }

        if (IsOpcode(value))
        {
            return new JmToken(JmTokenType.Opcode, value, startLine, startColumn);
        }

        if (IsDescriptor(value))
        {
            return new JmToken(JmTokenType.Descriptor, value, startLine, startColumn);
        }

        return new JmToken(JmTokenType.Identifier, value, startLine, startColumn);
    }

    #region 辅助方法

    private static bool IsOpcode(string value)
    {
        return value.Length >= 2 && !value.StartsWith('.') && !char.IsDigit(value[0]) && !AccessModifiers.Contains(value);
    }

    private static bool IsDescriptor(string value)
    {
        return value is "V" or "Z" or "B" or "S" or "I" or "J" or "F" or "D" or "C"
               || value.StartsWith("L") && value.EndsWith(";")
               || value.StartsWith("[");
    }

    private static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c is '_' or '/' or '$' or '<' or '>';
    }

    private static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '/' or '$' or '<' or '>' or '-' or ':';
    }

    private static bool IsHexDigit(char c)
    {
        return c is >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
        {
            if (Peek() == '\n')
            {
                _line++;
                _column = 0;
            }

            Advance();
        }
    }

    private bool IsAtEnd() => _position >= _source.Length;

    private char Peek(int offset = 0)
    {
        var index = _position + offset;
        return index < _source.Length ? _source[index] : '\0';
    }

    private char Advance()
    {
        var c = _source[_position++];
        _column++;
        return c;
    }

    #endregion
}
