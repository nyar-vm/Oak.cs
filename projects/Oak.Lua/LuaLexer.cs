namespace Oak.Lua;

/// <summary>
///     Lua 词法分析器。
/// </summary>
public sealed class LuaLexer
{
    private string _source;
    private int _position;
    private int _line;
    private int _column;

    /// <summary>
    ///     初始化 <see cref="LuaLexer" /> 的新实例。
    /// </summary>
    public LuaLexer()
    {
        _source = string.Empty;
        _position = 0;
        _line = 1;
        _column = 1;
    }

    /// <summary>
    ///     当前行号。
    /// </summary>
    public int Line => _line;

    /// <summary>
    ///     当前列号。
    /// </summary>
    public int Column => _column;

    /// <summary>
    ///     设置源文本。
    /// </summary>
    public void SetSource(string source)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
    }

    /// <summary>
    ///     读取下一个 Token。
    /// </summary>
    public (LuaTokenType Type, string Text, int Line, int Column) NextToken()
    {
        SkipWhitespace();

        if (_position >= _source.Length)
        {
            return (LuaTokenType.EndOfFile, string.Empty, _line, _column);
        }

        var ch = _source[_position];
        var startLine = _line;
        var startCol = _column;

        if (ch == '-' && Peek(1) == '-')
        {
            return ReadComment(startLine, startCol);
        }

        if (ch == '"' || ch == '\'')
        {
            return ReadShortString(startLine, startCol);
        }

        if (ch == '[' && (Peek(1) == '[' || Peek(1) == '='))
        {
            return ReadLongString(startLine, startCol);
        }

        if (char.IsDigit(ch) || (ch == '.' && Peek(1) != '.' && char.IsDigit(Peek(1))))
        {
            return ReadNumber(startLine, startCol);
        }

        if (IsNameStart(ch))
        {
            return ReadName(startLine, startCol);
        }

        return ReadSymbol(startLine, startCol);
    }

    private void SkipWhitespace()
    {
        while (_position < _source.Length)
        {
            var ch = _source[_position];

            if (ch == ' ' || ch == '\t' || ch == '\r')
            {
                Advance();
            }
            else if (ch == '\n')
            {
                Advance();
            }
            else
            {
                break;
            }
        }
    }

    private (LuaTokenType, string, int, int) ReadComment(int line, int col)
    {
        Advance();
        Advance();

        if (_position < _source.Length && _source[_position] == '[')
        {
            var level = CountLongBrackets();

            if (level >= 0)
            {
                return ReadLongCommentBody(level, line, col);
            }
        }

        var start = _position;

        while (_position < _source.Length && _source[_position] != '\n')
        {
            Advance();
        }

        return (LuaTokenType.Comment, _source[start.._position], line, col);
    }

    private (LuaTokenType, string, int, int) ReadLongCommentBody(int level, int line, int col)
    {
        var start = _position - 2;
        SkipLongStringBody(level);

        return (LuaTokenType.LongComment, _source[start.._position], line, col);
    }

    private (LuaTokenType, string, int, int) ReadShortString(int line, int col)
    {
        var quote = _source[_position];
        Advance();
        var start = _position;

        while (_position < _source.Length)
        {
            var ch = _source[_position];

            if (ch == '\\')
            {
                Advance();

                if (_position < _source.Length)
                {
                    Advance();
                }

                continue;
            }

            if (ch == quote)
            {
                break;
            }

            if (ch == '\n')
            {
                break;
            }

            Advance();
        }

        var text = _source[start.._position];

        if (_position < _source.Length && _source[_position] == quote)
        {
            Advance();
        }

        return (LuaTokenType.String, text, line, col);
    }

    private (LuaTokenType, string, int, int) ReadLongString(int line, int col)
    {
        var start = _position;
        var level = CountLongBrackets();

        if (level < 0)
        {
            return (LuaTokenType.LeftBracket, "[", line, col);
        }

        SkipLongStringBody(level);

        return (LuaTokenType.LongString, _source[start.._position], line, col);
    }

    private int CountLongBrackets()
    {
        if (_position >= _source.Length || _source[_position] != '[')
        {
            return -1;
        }

        var pos = _position + 1;
        var level = 0;

        while (pos < _source.Length && _source[pos] == '=')
        {
            level++;
            pos++;
        }

        if (pos < _source.Length && _source[pos] == '[')
        {
            _position = pos + 1;
            return level;
        }

        return -1;
    }

    private void SkipLongStringBody(int level)
    {
        while (_position < _source.Length)
        {
            if (_source[_position] == ']')
            {
                var pos = _position + 1;
                var eqCount = 0;

                while (pos < _source.Length && _source[pos] == '=' && eqCount < level)
                {
                    eqCount++;
                    pos++;
                }

                if (eqCount == level && pos < _source.Length && _source[pos] == ']')
                {
                    _position = pos + 1;
                    return;
                }
            }

            if (_source[_position] == '\n')
            {
                _line++;
                _column = 1;
            }

            _position++;
            _column++;
        }
    }

    private (LuaTokenType, string, int, int) ReadNumber(int line, int col)
    {
        var start = _position;
        var isFloat = false;

        if (_source[_position] == '0' && _position + 1 < _source.Length)
        {
            var next = char.ToLower(_source[_position + 1]);

            if (next == 'x')
            {
                Advance();
                Advance();

                while (_position < _source.Length && IsHexDigit(_source[_position]))
                {
                    Advance();
                }

                return (LuaTokenType.Integer, _source[start.._position], line, col);
            }

            if (next == 'b')
            {
                Advance();
                Advance();

                while (_position < _source.Length && (_source[_position] == '0' || _source[_position] == '1'))
                {
                    Advance();
                }

                return (LuaTokenType.Integer, _source[start.._position], line, col);
            }
        }

        while (_position < _source.Length && char.IsDigit(_source[_position]))
        {
            Advance();
        }

        if (_position < _source.Length && _source[_position] == '.')
        {
            if (_position + 1 < _source.Length && char.IsDigit(_source[_position + 1]))
            {
                isFloat = true;
                Advance();

                while (_position < _source.Length && char.IsDigit(_source[_position]))
                {
                    Advance();
                }
            }
        }

        if (_position < _source.Length && (char.ToLower(_source[_position]) == 'e' || char.ToLower(_source[_position]) == 'p'))
        {
            isFloat = true;
            Advance();

            if (_position < _source.Length && (_source[_position] == '+' || _source[_position] == '-'))
            {
                Advance();
            }

            while (_position < _source.Length && char.IsDigit(_source[_position]))
            {
                Advance();
            }
        }

        return (isFloat ? LuaTokenType.Float : LuaTokenType.Integer, _source[start.._position], line, col);
    }

    private (LuaTokenType, string, int, int) ReadName(int line, int col)
    {
        var start = _position;

        while (_position < _source.Length && IsNameChar(_source[_position]))
        {
            Advance();
        }

        var text = _source[start.._position];
        var type = ClassifyKeyword(text);

        return (type, text, line, col);
    }

    private (LuaTokenType, string, int, int) ReadSymbol(int line, int col)
    {
        var ch = _source[_position];

        var type = ch switch
        {
            '+' => LuaTokenType.Plus,
            '*' => LuaTokenType.Star,
            '/' => LuaTokenType.Slash,
            '%' => LuaTokenType.Percent,
            '^' => LuaTokenType.Caret,
            '#' => LuaTokenType.Hash,
            '(' => LuaTokenType.LeftParen,
            ')' => LuaTokenType.RightParen,
            '{' => LuaTokenType.LeftBrace,
            '}' => LuaTokenType.RightBrace,
            ']' => LuaTokenType.RightBracket,
            ';' => LuaTokenType.Semicolon,
            ',' => LuaTokenType.Comma,
            _ => LuaTokenType.EndOfFile
        };

        if (ch == '=' && Peek(1) == '=')
        {
            Advance();
            Advance();
            return (LuaTokenType.EqualEqual, "==", line, col);
        }

        if (ch == '~' && Peek(1) == '=')
        {
            Advance();
            Advance();
            return (LuaTokenType.TildeEqual, "~=", line, col);
        }

        if (ch == '<' && Peek(1) == '=')
        {
            Advance();
            Advance();
            return (LuaTokenType.LessEqual, "<=", line, col);
        }

        if (ch == '>' && Peek(1) == '=')
        {
            Advance();
            Advance();
            return (LuaTokenType.GreaterEqual, ">=", line, col);
        }

        if (ch == '<')
        {
            Advance();
            return (LuaTokenType.Less, "<", line, col);
        }

        if (ch == '>')
        {
            Advance();
            return (LuaTokenType.Greater, ">", line, col);
        }

        if (ch == '=')
        {
            Advance();
            return (LuaTokenType.Equal, "=", line, col);
        }

        if (ch == '-' )
        {
            Advance();
            return (LuaTokenType.Minus, "-", line, col);
        }

        if (ch == '.' && Peek(1) == '.' && Peek(2) == '.')
        {
            Advance();
            Advance();
            Advance();
            return (LuaTokenType.Dots, "...", line, col);
        }

        if (ch == '.' && Peek(1) == '.')
        {
            Advance();
            Advance();
            return (LuaTokenType.Concat, "..", line, col);
        }

        if (ch == '.')
        {
            Advance();
            return (LuaTokenType.Dot, ".", line, col);
        }

        if (ch == ':' && Peek(1) == ':')
        {
            Advance();
            Advance();
            return (LuaTokenType.DoubleColon, "::", line, col);
        }

        if (ch == ':')
        {
            Advance();
            return (LuaTokenType.Colon, ":", line, col);
        }

        if (ch == '[')
        {
            Advance();
            return (LuaTokenType.LeftBracket, "[", line, col);
        }

        Advance();

        return (type, ch.ToString(), line, col);
    }

    private static LuaTokenType ClassifyKeyword(string text)
    {
        return text switch
        {
            "and" => LuaTokenType.And,
            "break" => LuaTokenType.Break,
            "do" => LuaTokenType.Do,
            "else" => LuaTokenType.Else,
            "elseif" => LuaTokenType.ElseIf,
            "end" => LuaTokenType.End,
            "false" => LuaTokenType.False,
            "for" => LuaTokenType.For,
            "function" => LuaTokenType.Function,
            "goto" => LuaTokenType.Goto,
            "if" => LuaTokenType.If,
            "in" => LuaTokenType.In,
            "local" => LuaTokenType.Local,
            "nil" => LuaTokenType.Nil,
            "not" => LuaTokenType.Not,
            "or" => LuaTokenType.Or,
            "repeat" => LuaTokenType.Repeat,
            "return" => LuaTokenType.Return,
            "then" => LuaTokenType.Then,
            "true" => LuaTokenType.True,
            "until" => LuaTokenType.Until,
            "while" => LuaTokenType.While,
            _ => LuaTokenType.Name
        };
    }

    private char Peek(int offset)
    {
        var pos = _position + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }

    private void Advance()
    {
        if (_position < _source.Length)
        {
            if (_source[_position] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }

            _position++;
        }
    }

    private static bool IsNameStart(char ch) => char.IsLetter(ch) || ch == '_';

    private static bool IsNameChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_';

    private static bool IsHexDigit(char ch) => char.IsDigit(ch) || ch is >= 'a' and <= 'f' or >= 'A' and <= 'F';
}
