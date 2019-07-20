using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Javap;

/// <summary>
///     Javap 词法分析器
///     将 javap -c 输出文本分解为 Token 流
/// </summary>
public sealed class JvpLexer
{
    private string _source = "";
    private int _position;
    private int _line;
    private int _column;
    private DiagnosticSink? _diagnostics;

    private static readonly HashSet<string> AccessModifiers =
    [
        "public", "private", "protected", "static", "final",
        "synchronized", "volatile", "transient", "native",
        "abstract", "strictfp", "enum", "interface"
    ];

    private static readonly HashSet<string> TypeKeywords = ["class", "interface", "enum", "record", "module"];

    /// <summary>
    ///     词法分析
    /// </summary>
    /// <param name="source">javap -c 输出文本</param>
    /// <param name="diagnostics">诊断接收器</param>
    /// <returns>Token 列表</returns>
    public IReadOnlyList<JvpToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<JvpToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();
            if (IsAtEnd()) break;

            var token = ScanToken();
            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new JvpToken(JvpTokenType.Eof, "", _line, _column));
        return tokens;
    }

    /// <summary>
    ///     扫描下一个 Token
    /// </summary>
    private JvpToken? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;

        if (Peek() == '/' && Peek(1) == '/')
        {
            return ScanComment(startLine, startColumn);
        }

        if (Peek() == '#')
        {
            return ScanConstantPoolRef(startLine, startColumn);
        }

        if (Peek() is '{' or '}' or '(' or ')' or ';' or ':' or ',' or '.' or '[' or ']')
        {
            var c = Advance();
            return new JvpToken(JvpTokenType.Punctuation, c.ToString(), startLine, startColumn);
        }

        if (char.IsDigit(Peek()) || (Peek() == '-' && Peek(1) is not '\0' && char.IsDigit(Peek(1))))
        {
            return ScanNumber(startLine, startColumn);
        }

        if (IsIdentifierStart(Peek()))
        {
            return ScanWord(startLine, startColumn);
        }

        _diagnostics?.AddWarning("", default, "JVP1001", $"未识别的字符 '{Peek()}'");
        Advance();
        return null;
    }

    /// <summary>
    ///     扫描注释
    /// </summary>
    private JvpToken ScanComment(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '\n')
        {
            sb.Append(Advance());
        }

        return new JvpToken(JvpTokenType.Comment, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描常量池引用
    /// </summary>
    private JvpToken ScanConstantPoolRef(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            sb.Append(Advance());
        }

        return new JvpToken(JvpTokenType.ConstantPoolRef, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描数字
    /// </summary>
    private JvpToken ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (Peek() == '-') sb.Append(Advance());

        while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == 'x' || IsHexDigit(Peek())))
        {
            sb.Append(Advance());
        }

        return new JvpToken(JvpTokenType.Number, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描单词（标识符、操作码、关键字等）
    /// </summary>
    private JvpToken ScanWord(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            sb.Append(Advance());
        }

        var value = sb.ToString();

        if (AccessModifiers.Contains(value))
        {
            return new JvpToken(JvpTokenType.AccessModifier, value, startLine, startColumn);
        }

        if (TypeKeywords.Contains(value))
        {
            return new JvpToken(JvpTokenType.TypeKeyword, value, startLine, startColumn);
        }

        if (value == "Compiled")
        {
            return new JvpToken(JvpTokenType.HeaderKeyword, value, startLine, startColumn);
        }

        if (value == "Code")
        {
            return new JvpToken(JvpTokenType.SectionMarker, value, startLine, startColumn);
        }

        if (IsJvmOpcode(value))
        {
            return new JvpToken(JvpTokenType.Opcode, value, startLine, startColumn);
        }

        return new JvpToken(JvpTokenType.Identifier, value, startLine, startColumn);
    }

    #region 辅助方法

    private static bool IsJvmOpcode(string value)
    {
        return (value.Length >= 2 && value.Contains('_')) || value is
            "nop" or "aconst_null" or "iconst_m1" or "return" or "areturn"
            or "ireturn" or "lreturn" or "freturn" or "dreturn" or "dup" or "pop"
            or "swap" or "iadd" or "isub" or "imul" or "idiv" or "ineg" or "iand"
            or "ior" or "ixor" or "ishl" or "ishr" or "iushr" or "ladd" or "lsub"
            or "lmul" or "ldiv" or "i2l" or "i2f" or "i2d" or "l2i" or "l2f" or "l2d"
            or "f2i" or "f2l" or "f2d" or "d2i" or "d2l" or "d2f" or "lcmp" or "new"
            or "athrow" or "monitorenter" or "monitorexit" or "arraylength"
            or "checkcast" or "instanceof" or "ifnull" or "ifnonnull";
    }

    private static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c is '_' or '$' or '<' or '>';
    }

    private static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '$' or '<' or '>' or '.';
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
