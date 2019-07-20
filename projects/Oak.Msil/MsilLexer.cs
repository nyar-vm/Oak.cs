using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Msil;

/// <summary>
///     ILASM 词法分析器
///     将 ILASM 源码文本分解为 Token 流
/// </summary>
public sealed class MsilLexer
{
    private string _source = "";
    private int _position;
    private int _line;
    private int _column;
    private DiagnosticSink? _diagnostics;

    private static readonly HashSet<string> AccessModifiers =
    [
        "public", "private", "family", "assembly", "famandassem",
        "famorassem", "privatescope", "static", "instance", "virtual",
        "abstract", "sealed", "final", "specialname", "rtspecialname",
        "initonly", "literal", "notserialized", "value", "enum",
        "interface", "sequential", "auto", "explicit", "ansi",
        "unicode", "autochar", "beforefieldinit", "cil", "managed",
        "unmanaged", "forwardref", "preservesig", "internalcall",
        "synchronized", "noinlining", "aggressiveinlining", "optil",
        "nooptimization"
    ];

    /// <summary>
    ///     词法分析
    /// </summary>
    /// <param name="source">ILASM 源码文本</param>
    /// <param name="diagnostics">诊断接收器</param>
    /// <returns>Token 列表</returns>
    public IReadOnlyList<MsilToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<MsilToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();
            if (IsAtEnd()) break;

            var token = ScanToken();
            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new MsilToken(MsilTokenType.Eof, "", _line, _column));
        return tokens;
    }

    /// <summary>
    ///     扫描下一个 Token
    /// </summary>
    private MsilToken? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;

        if (Peek() == '/' && Peek(1) == '/')
        {
            return ScanComment(startLine, startColumn);
        }

        if (Peek() == '"')
        {
            return ScanString(startLine, startColumn);
        }

        if (Peek() == '.')
        {
            if (Peek(1) is not '\0' && char.IsLetter(Peek(1)))
            {
                return ScanDirectiveOrOpcode(startLine, startColumn);
            }

            Advance();
            return null;
        }

        if (Peek() is '{' or '}' or '(' or ')' or ';' or ':' or ',' or '[' or ']' or '=')
        {
            var c = Advance();
            return new MsilToken(MsilTokenType.Punctuation, c.ToString(), startLine, startColumn);
        }

        if (char.IsDigit(Peek()) || (Peek() == '-' && Peek(1) is not '\0' && char.IsDigit(Peek(1))))
        {
            return ScanNumber(startLine, startColumn);
        }

        if (IsIdentifierStart(Peek()))
        {
            return ScanWord(startLine, startColumn);
        }

        _diagnostics?.AddWarning("", default, "IL1001", $"未识别的字符 '{Peek()}'");
        Advance();
        return null;
    }

    /// <summary>
    ///     扫描注释
    /// </summary>
    private MsilToken ScanComment(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '\n')
        {
            sb.Append(Advance());
        }

        return new MsilToken(MsilTokenType.Comment, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描字符串字面量
    /// </summary>
    private MsilToken ScanString(int startLine, int startColumn)
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

        return new MsilToken(MsilTokenType.StringLiteral, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描指令关键字或操作码（. 开头）
    /// </summary>
    private MsilToken ScanDirectiveOrOpcode(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() is '.' or '_'))
        {
            sb.Append(Advance());
        }

        var value = sb.ToString();

        if (value.StartsWith("."))
        {
            return new MsilToken(MsilTokenType.Directive, value, startLine, startColumn);
        }

        return new MsilToken(MsilTokenType.Opcode, value, startLine, startColumn);
    }

    /// <summary>
    ///     扫描数字
    /// </summary>
    private MsilToken ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (Peek() == '-') sb.Append(Advance());

        while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == 'x' || IsHexDigit(Peek())))
        {
            sb.Append(Advance());
        }

        return new MsilToken(MsilTokenType.Number, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    ///     扫描单词
    /// </summary>
    private MsilToken ScanWord(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (Peek() == 'I' && Peek(1) == 'L' && Peek(2) == '_')
        {
            sb.Append(Advance());
            sb.Append(Advance());
            sb.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }

            if (!IsAtEnd() && Peek() == ':') sb.Append(Advance());

            return new MsilToken(MsilTokenType.IlLabel, sb.ToString(), startLine, startColumn);
        }

        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            sb.Append(Advance());
        }

        var value = sb.ToString();

        if (AccessModifiers.Contains(value))
        {
            return new MsilToken(MsilTokenType.AccessModifier, value, startLine, startColumn);
        }

        if (IsMsilOpcode(value))
        {
            return new MsilToken(MsilTokenType.Opcode, value, startLine, startColumn);
        }

        if (IsTypeReference(value))
        {
            return new MsilToken(MsilTokenType.TypeReference, value, startLine, startColumn);
        }

        return new MsilToken(MsilTokenType.Identifier, value, startLine, startColumn);
    }

    #region 辅助方法

    private static bool IsMsilOpcode(string value)
    {
        return value.Contains('.') && !value.StartsWith(".") && char.IsLetter(value[0]);
    }

    private static bool IsTypeReference(string value)
    {
        return value is "void" or "bool" or "int8" or "int16" or "int32" or "int64"
               or "unsigned.int8" or "unsigned.int16" or "unsigned.int32" or "unsigned.int64"
               or "float32" or "float64" or "string" or "object" or "native" or "typedref";
    }

    private static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c is '_' or '$' or '<' or '>';
    }

    private static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '$' or '<' or '>' or '.' or '`';
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
