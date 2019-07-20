using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Glsl;

public sealed class GlslLexer
{
    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public IReadOnlyList<GlslToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<GlslToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();

            if (IsAtEnd()) break;

            var token = ScanToken();

            if (token.Type != GlslTokenType.Invalid)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new GlslToken(GlslTokenType.EndOfFile, string.Empty, _line, _column));
        return tokens;
    }

    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    }

    private char Peek()
    {
        return IsAtEnd() ? '\0' : _source[_position];
    }

    private char PeekNext()
    {
        return _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
    }

    private char Advance()
    {
        var c = _source[_position];
        _position++;

        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return c;
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_position] != expected) return false;

        Advance();
        return true;
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek())) Advance();
    }

    private GlslToken ScanToken()
    {
        var line = _line;
        var column = _column;
        var c = Peek();

        if (c == '/' && PeekNext() == '/')
        {
            return ScanLineComment(line, column);
        }

        if (c == '/' && PeekNext() == '*')
        {
            return ScanBlockComment(line, column);
        }

        if (c == '#')
        {
            return ScanPreprocessor(line, column);
        }

        if (c == '"')
        {
            return ScanString(line, column);
        }

        if (char.IsDigit(c) || (c == '.' && char.IsDigit(PeekNext())))
        {
            return ScanNumber(line, column);
        }

        if (IsIdentifierStart(c))
        {
            return ScanIdentifier(line, column);
        }

        return ScanSymbol(line, column);
    }

    #region 注释与预处理

    private GlslToken ScanLineComment(int line, int column)
    {
        Advance();
        Advance();

        var sb = new StringBuilder("//");

        while (!IsAtEnd() && Peek() != '\n')
        {
            sb.Append(Advance());
        }

        return new GlslToken(GlslTokenType.LineComment, sb.ToString(), line, column);
    }

    private GlslToken ScanBlockComment(int line, int column)
    {
        Advance();
        Advance();

        var sb = new StringBuilder("/*");

        while (!IsAtEnd())
        {
            if (Peek() == '*' && PeekNext() == '/')
            {
                sb.Append(Advance());
                sb.Append(Advance());
                break;
            }

            sb.Append(Advance());
        }

        return new GlslToken(GlslTokenType.BlockComment, sb.ToString(), line, column);
    }

    private GlslToken ScanPreprocessor(int line, int column)
    {
        Advance();

        var sb = new StringBuilder("#");

        while (!IsAtEnd() && Peek() != '\n')
        {
            if (Peek() == '\\' && PeekNext() == '\n')
            {
                sb.Append(Advance());
                sb.Append(Advance());
                continue;
            }

            sb.Append(Advance());
        }

        return new GlslToken(GlslTokenType.Preprocessor, sb.ToString(), line, column);
    }

    #endregion

    #region 字符串

    private GlslToken ScanString(int line, int column)
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\' && !IsAtEnd())
            {
                Advance();
                if (!IsAtEnd()) sb.Append(Advance());
                continue;
            }

            sb.Append(Advance());
        }

        if (!IsAtEnd()) Advance();

        return new GlslToken(GlslTokenType.String, sb.ToString(), line, column);
    }

    #endregion

    #region 数字

    private GlslToken ScanNumber(int line, int column)
    {
        var start = _position;

        if (Peek() == '0' && PeekNext() is 'x' or 'X')
        {
            Advance();
            Advance();

            while (!IsAtEnd() && IsHexDigit(Peek())) Advance();

            return new GlslToken(GlslTokenType.IntConstant, _source[start.._position], line, column);
        }

        var isFloat = false;

        if (Peek() != '.')
        {
            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
        {
            isFloat = true;
            Advance();

            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            isFloat = true;
            Advance();

            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) Advance();

            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        if (!IsAtEnd())
        {
            var suffix = char.ToLower(Peek());

            if (suffix == 'f')
            {
                isFloat = true;
                Advance();
            }
            else if (suffix == 'l')
            {
                Advance();

                return new GlslToken(GlslTokenType.DoubleConstant, _source[start.._position], line, column);
            }
            else if (suffix == 'u')
            {
                Advance();

                return new GlslToken(GlslTokenType.UintConstant, _source[start.._position], line, column);
            }
        }

        return new GlslToken(
            isFloat ? GlslTokenType.FloatConstant : GlslTokenType.IntConstant,
            _source[start.._position], line, column);
    }

    #endregion

    #region 标识符与关键字

    private GlslToken ScanIdentifier(int line, int column)
    {
        var start = _position;

        while (!IsAtEnd() && IsIdentifierChar(Peek())) Advance();

        var text = _source[start.._position];
        var type = ClassifyKeyword(text);

        return new GlslToken(type, text, line, column);
    }

    private static GlslTokenType ClassifyKeyword(string text)
    {
        return text switch
        {
            "void" => GlslTokenType.Void,
            "float" => GlslTokenType.Float,
            "double" => GlslTokenType.Double,
            "int" => GlslTokenType.Int,
            "uint" => GlslTokenType.Uint,
            "bool" => GlslTokenType.Bool,
            "vec2" or "vec3" or "vec4" => GlslTokenType.Vec,
            "mat2" or "mat3" or "mat4" => GlslTokenType.Mat,
            "dmat2" or "dmat3" or "dmat4" => GlslTokenType.DMat,
            "ivec2" or "ivec3" or "ivec4" => GlslTokenType.IVec,
            "uvec2" or "uvec3" or "uvec4" => GlslTokenType.UVec,
            "bvec2" or "bvec3" or "bvec4" => GlslTokenType.BVec,
            "sampler2D" or "samplerCube" or "sampler2DShadow"
                or "samplerCubeShadow" or "sampler2DArray"
                or "sampler2DArrayShadow" or "sampler3D" or "sampler2DRect" => GlslTokenType.Sampler,
            "isampler2D" or "isamplerCube" or "isampler2DArray"
                or "isampler3D" or "isampler2DRect" or "isampler2DShadow"
                or "isamplerCubeShadow" or "isampler2DArrayShadow" => GlslTokenType.ISampler,
            "usampler2D" or "usamplerCube" or "usampler2DArray"
                or "usampler3D" or "usampler2DRect" or "usampler2DShadow"
                or "usamplerCubeShadow" or "usampler2DArrayShadow" => GlslTokenType.USampler,
            "image2D" or "image3D" or "imageCube" or "image2DArray"
                or "imageBuffer" or "image2DRect" or "iimage2D"
                or "uimage2D" or "image2DMS" => GlslTokenType.Image,
            "attribute" => GlslTokenType.Attribute,
            "varying" => GlslTokenType.Varying,
            "uniform" => GlslTokenType.Uniform,
            "in" => GlslTokenType.In,
            "out" => GlslTokenType.Out,
            "inout" => GlslTokenType.Inout,
            "const" => GlslTokenType.Const,
            "layout" => GlslTokenType.Layout,
            "struct" => GlslTokenType.Struct,
            "precision" => GlslTokenType.Precision,
            "highp" => GlslTokenType.Highp,
            "mediump" => GlslTokenType.Mediump,
            "lowp" => GlslTokenType.Lowp,
            "flat" => GlslTokenType.Flat,
            "smooth" => GlslTokenType.Smooth,
            "noperspective" => GlslTokenType.Noperspective,
            "centroid" => GlslTokenType.Centroid,
            "patch" => GlslTokenType.Patch,
            "sample" => GlslTokenType.Sample,
            "subroutine" => GlslTokenType.Subroutine,
            "coherent" => GlslTokenType.Coherent,
            "volatile" => GlslTokenType.Volatile,
            "restrict" => GlslTokenType.Restrict,
            "readonly" => GlslTokenType.Readonly,
            "writeonly" => GlslTokenType.Writeonly,
            "buffer" => GlslTokenType.Buffer,
            "if" => GlslTokenType.If,
            "else" => GlslTokenType.Else,
            "for" => GlslTokenType.For,
            "while" => GlslTokenType.While,
            "do" => GlslTokenType.Do,
            "return" => GlslTokenType.Return,
            "discard" => GlslTokenType.Discard,
            "break" => GlslTokenType.Break,
            "continue" => GlslTokenType.Continue,
            "switch" => GlslTokenType.Switch,
            "case" => GlslTokenType.Case,
            "default" => GlslTokenType.Default,
            "true" or "false" => GlslTokenType.BoolConstant,
            _ => GlslTokenType.Identifier
        };
    }

    #endregion

    #region 运算符与符号

    private GlslToken ScanSymbol(int line, int column)
    {
        var c = Advance();

        switch (c)
        {
            case '{': return new GlslToken(GlslTokenType.LeftBrace, "{", line, column);
            case '}': return new GlslToken(GlslTokenType.RightBrace, "}", line, column);
            case '(': return new GlslToken(GlslTokenType.LeftParen, "(", line, column);
            case ')': return new GlslToken(GlslTokenType.RightParen, ")", line, column);
            case '[': return new GlslToken(GlslTokenType.LeftBracket, "[", line, column);
            case ']': return new GlslToken(GlslTokenType.RightBracket, "]", line, column);
            case ';': return new GlslToken(GlslTokenType.Semicolon, ";", line, column);
            case ',': return new GlslToken(GlslTokenType.Comma, ",", line, column);
            case ':': return new GlslToken(GlslTokenType.Colon, ":", line, column);
            case '.': return new GlslToken(GlslTokenType.Dot, ".", line, column);
            case '?': return new GlslToken(GlslTokenType.Question, "?", line, column);
            case '~': return new GlslToken(GlslTokenType.Tilde, "~", line, column);

            case '+':
                if (Match('=')) return new GlslToken(GlslTokenType.PlusEqual, "+=", line, column);
                if (Match('+')) return new GlslToken(GlslTokenType.PlusPlus, "++", line, column);
                return new GlslToken(GlslTokenType.Plus, "+", line, column);

            case '-':
                if (Match('=')) return new GlslToken(GlslTokenType.MinusEqual, "-=", line, column);
                if (Match('-')) return new GlslToken(GlslTokenType.MinusMinus, "--", line, column);
                return new GlslToken(GlslTokenType.Minus, "-", line, column);

            case '*':
                if (Match('=')) return new GlslToken(GlslTokenType.StarEqual, "*=", line, column);
                return new GlslToken(GlslTokenType.Star, "*", line, column);

            case '/':
                if (Match('=')) return new GlslToken(GlslTokenType.SlashEqual, "/=", line, column);
                return new GlslToken(GlslTokenType.Slash, "/", line, column);

            case '%':
                if (Match('=')) return new GlslToken(GlslTokenType.PercentEqual, "%=", line, column);
                return new GlslToken(GlslTokenType.Percent, "%", line, column);

            case '&':
                if (Match('&')) return new GlslToken(GlslTokenType.LogicalAnd, "&&", line, column);
                if (Match('=')) return new GlslToken(GlslTokenType.AmpersandEqual, "&=", line, column);
                return new GlslToken(GlslTokenType.Ampersand, "&", line, column);

            case '|':
                if (Match('|')) return new GlslToken(GlslTokenType.LogicalOr, "||", line, column);
                if (Match('=')) return new GlslToken(GlslTokenType.PipeEqual, "|=", line, column);
                return new GlslToken(GlslTokenType.Pipe, "|", line, column);

            case '^':
                if (Match('=')) return new GlslToken(GlslTokenType.CaretEqual, "^=", line, column);
                return new GlslToken(GlslTokenType.Caret, "^", line, column);

            case '=':
                if (Match('=')) return new GlslToken(GlslTokenType.EqualEqual, "==", line, column);
                return new GlslToken(GlslTokenType.Equal, "=", line, column);

            case '!':
                if (Match('=')) return new GlslToken(GlslTokenType.NotEqual, "!=", line, column);
                return new GlslToken(GlslTokenType.Not, "!", line, column);

            case '<':
                if (Match('<'))
                {
                    if (Match('=')) return new GlslToken(GlslTokenType.LeftShiftEqual, "<<=", line, column);
                    return new GlslToken(GlslTokenType.LeftShift, "<<", line, column);
                }

                if (Match('=')) return new GlslToken(GlslTokenType.LessEqual, "<=", line, column);
                return new GlslToken(GlslTokenType.Less, "<", line, column);

            case '>':
                if (Match('>'))
                {
                    if (Match('=')) return new GlslToken(GlslTokenType.RightShiftEqual, ">>=", line, column);
                    return new GlslToken(GlslTokenType.RightShift, ">>", line, column);
                }

                if (Match('=')) return new GlslToken(GlslTokenType.GreaterEqual, ">=", line, column);
                return new GlslToken(GlslTokenType.Greater, ">", line, column);

            default:
                _diagnostics?.AddError(
                    string.Empty,
                    default,
                    "GLSL001",
                    $"意外的字符 '{c}'");
                return new GlslToken(GlslTokenType.Invalid, c.ToString(), line, column);
        }
    }

    #endregion

    private static bool IsIdentifierStart(char ch) => char.IsLetter(ch) || ch == '_';

    private static bool IsIdentifierChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_';

    private static bool IsHexDigit(char ch) => char.IsDigit(ch) || ch is >= 'a' and <= 'f' or >= 'A' and <= 'F';
}
