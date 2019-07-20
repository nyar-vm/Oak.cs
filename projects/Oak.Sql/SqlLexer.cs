using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Sql;

public sealed class SqlLexer
{
    private static readonly Dictionary<string, SqlTokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SELECT"] = SqlTokenType.Select,
        ["FROM"] = SqlTokenType.From,
        ["WHERE"] = SqlTokenType.Where,
        ["INSERT"] = SqlTokenType.Insert,
        ["INTO"] = SqlTokenType.Into,
        ["VALUES"] = SqlTokenType.Values,
        ["UPDATE"] = SqlTokenType.Update,
        ["SET"] = SqlTokenType.Set,
        ["DELETE"] = SqlTokenType.Delete,
        ["DROP"] = SqlTokenType.Drop,
        ["CREATE"] = SqlTokenType.Create,
        ["TABLE"] = SqlTokenType.Table,
        ["INDEX"] = SqlTokenType.Index,
        ["JOIN"] = SqlTokenType.Join,
        ["INNER"] = SqlTokenType.Inner,
        ["LEFT"] = SqlTokenType.Left,
        ["RIGHT"] = SqlTokenType.Right,
        ["OUTER"] = SqlTokenType.Outer,
        ["FULL"] = SqlTokenType.Full,
        ["CROSS"] = SqlTokenType.Cross,
        ["NATURAL"] = SqlTokenType.Natural,
        ["ON"] = SqlTokenType.On,
        ["AND"] = SqlTokenType.And,
        ["OR"] = SqlTokenType.Or,
        ["NOT"] = SqlTokenType.Not,
        ["AS"] = SqlTokenType.As,
        ["ORDER"] = SqlTokenType.Order,
        ["BY"] = SqlTokenType.By,
        ["GROUP"] = SqlTokenType.Group,
        ["HAVING"] = SqlTokenType.Having,
        ["LIMIT"] = SqlTokenType.Limit,
        ["OFFSET"] = SqlTokenType.Offset,
        ["DISTINCT"] = SqlTokenType.Distinct,
        ["ALL"] = SqlTokenType.All,
        ["NULL"] = SqlTokenType.Null,
        ["IS"] = SqlTokenType.Is,
        ["IN"] = SqlTokenType.In,
        ["BETWEEN"] = SqlTokenType.Between,
        ["LIKE"] = SqlTokenType.Like,
        ["ILIKE"] = SqlTokenType.ILike,
        ["EXISTS"] = SqlTokenType.Exists,
        ["ASC"] = SqlTokenType.Asc,
        ["DESC"] = SqlTokenType.Desc,
        ["PRIMARY"] = SqlTokenType.Primary,
        ["KEY"] = SqlTokenType.Key,
        ["FOREIGN"] = SqlTokenType.Foreign,
        ["REFERENCES"] = SqlTokenType.References,
        ["DEFAULT"] = SqlTokenType.Default,
        ["CONSTRAINT"] = SqlTokenType.Constraint,
        ["UNIQUE"] = SqlTokenType.Unique,
        ["CHECK"] = SqlTokenType.Check,
        ["IF"] = SqlTokenType.If,
        ["INTEGER"] = SqlTokenType.Integer,
        ["INT"] = SqlTokenType.Integer,
        ["REAL"] = SqlTokenType.Real,
        ["TEXT"] = SqlTokenType.Text,
        ["BLOB"] = SqlTokenType.Blob,
        ["VARCHAR"] = SqlTokenType.Varchar,
        ["BOOLEAN"] = SqlTokenType.Boolean,
        ["BOOL"] = SqlTokenType.Boolean,
        ["DATE"] = SqlTokenType.Date,
        ["TIMESTAMP"] = SqlTokenType.Timestamp,
        ["ALTER"] = SqlTokenType.Alter,
        ["ADD"] = SqlTokenType.Add,
        ["COLUMN"] = SqlTokenType.Column,
        ["RENAME"] = SqlTokenType.Rename,
        ["TO"] = SqlTokenType.To,
        ["REPLACE"] = SqlTokenType.Replace,
        ["IGNORE"] = SqlTokenType.Ignore,
        ["UNION"] = SqlTokenType.Union,
        ["INTERSECT"] = SqlTokenType.Intersect,
        ["EXCEPT"] = SqlTokenType.Except,
        ["CASE"] = SqlTokenType.Case,
        ["WHEN"] = SqlTokenType.When,
        ["THEN"] = SqlTokenType.Then,
        ["ELSE"] = SqlTokenType.Else,
        ["IF"] = SqlTokenType.If,
        ["ELSIF"] = SqlTokenType.Elsif,
        ["END"] = SqlTokenType.End,
        ["PREPARE"] = SqlTokenType.Prepare,
        ["EXECUTE"] = SqlTokenType.Execute,
        ["DEALLOCATE"] = SqlTokenType.Deallocate,
        ["USING"] = SqlTokenType.Using,
        ["LIMIT"] = SqlTokenType.Limit,
        ["OFFSET"] = SqlTokenType.Offset,
        ["MATERIALIZED"] = SqlTokenType.Materialized,
        ["VIEW"] = SqlTokenType.View,
        ["REFRESH"] = SqlTokenType.Refresh,
        ["COMPLETE"] = SqlTokenType.Complete,
        ["FAST"] = SqlTokenType.Fast,
        ["CAST"] = SqlTokenType.Cast,
        ["COLLATE"] = SqlTokenType.Collate,
        ["AUTOINCREMENT"] = SqlTokenType.Autoincrement,
        ["RETURNING"] = SqlTokenType.Returning,
        ["TRUE"] = SqlTokenType.True,
        ["FALSE"] = SqlTokenType.False,
        ["BIGINT"] = SqlTokenType.BigInt,
        ["SMALLINT"] = SqlTokenType.SmallInt,
        ["TINYINT"] = SqlTokenType.TinyInt,
        ["FLOAT"] = SqlTokenType.Float,
        ["DOUBLE"] = SqlTokenType.Double,
        ["NUMERIC"] = SqlTokenType.Numeric,
        ["DECIMAL"] = SqlTokenType.Decimal,
        ["CHAR"] = SqlTokenType.Char,
        ["NCHAR"] = SqlTokenType.NChar,
        ["BINARY"] = SqlTokenType.Binary,
        ["GLOB"] = SqlTokenType.Glob,
        ["CONFLICT"] = SqlTokenType.Conflict,
        ["DO"] = SqlTokenType.Do,
        ["NOTHING"] = SqlTokenType.Nothing,
        ["UPSERT"] = SqlTokenType.Upsert,
        ["BEGIN"] = SqlTokenType.Begin,
        ["FUNCTION"] = SqlTokenType.Function,
        ["PROCEDURE"] = SqlTokenType.Procedure,
        ["CALL"] = SqlTokenType.Call,
        ["RETURNS"] = SqlTokenType.Returns,
        ["EXCLUDED"] = SqlTokenType.Excluded,
        ["SHOW"] = SqlTokenType.Show,
        ["TABLES"] = SqlTokenType.Tables,
        ["DESCRIBE"] = SqlTokenType.Describe,
        ["COLUMNS"] = SqlTokenType.Columns,
    };

    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public IReadOnlyList<SqlToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<SqlToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();

            if (IsAtEnd()) break;

            if (Peek() == '-' && PeekNext() == '-')
            {
                SkipLineComment();
                continue;
            }

            if (Peek() == '/' && PeekNext() == '*')
            {
                SkipBlockComment();
                continue;
            }

            var token = ScanToken();
            if (token.Type != SqlTokenType.Invalid) tokens.Add(token);
        }

        tokens.Add(new SqlToken(SqlTokenType.EndOfFile, string.Empty, _line, _column));
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

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek())) Advance();
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n') Advance();
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
            _diagnostics?.AddError(string.Empty, default,
                "SQL003", "未闭合的块注释");
        }
    }

    private SqlToken ScanToken()
    {
        var line = _line;
        var column = _column;
        var c = Peek();

        switch (c)
        {
            case '*':
                Advance();
                return new SqlToken(SqlTokenType.Star, "*", line, column);
            case ',':
                Advance();
                return new SqlToken(SqlTokenType.Comma, ",", line, column);
            case '(':
                Advance();
                return new SqlToken(SqlTokenType.LeftParen, "(", line, column);
            case ')':
                Advance();
                return new SqlToken(SqlTokenType.RightParen, ")", line, column);
            case ';':
                Advance();
                return new SqlToken(SqlTokenType.Semicolon, ";", line, column);
            case '.':
                Advance();
                return new SqlToken(SqlTokenType.Dot, ".", line, column);
            case '=':
                Advance();
                return new SqlToken(SqlTokenType.Equal, "=", line, column);
            case '+':
                Advance();
                return new SqlToken(SqlTokenType.Plus, "+", line, column);
            case '-':
                Advance();
                if (Peek() == '-')
                {
                    SkipLineComment();
                    return ScanToken();
                }

                return new SqlToken(SqlTokenType.Minus, "-", line, column);
            case '/':
                Advance();
                if (Peek() == '*')
                {
                    SkipBlockComment();
                    return ScanToken();
                }

                return new SqlToken(SqlTokenType.Slash, "/", line, column);
            case '%':
                Advance();
                return new SqlToken(SqlTokenType.Percent, "%", line, column);
            case '&':
                Advance();
                return new SqlToken(SqlTokenType.Ampersand, "&", line, column);
            case '|':
                Advance();
                if (Peek() == '|')
                {
                    Advance();
                    return new SqlToken(SqlTokenType.Concat, "||", line, column);
                }

                return new SqlToken(SqlTokenType.Pipe, "|", line, column);
            case '~':
                Advance();
                return new SqlToken(SqlTokenType.Tilde, "~", line, column);
            case '<':
                Advance();
                if (Peek() == '=')
                {
                    Advance();
                    return new SqlToken(SqlTokenType.LessEqual, "<=", line, column);
                }

                if (Peek() == '>')
                {
                    Advance();
                    return new SqlToken(SqlTokenType.NotEqual, "<>", line, column);
                }

                if (Peek() == '<')
                {
                    Advance();
                    return new SqlToken(SqlTokenType.LeftShift, "<<", line, column);
                }

                return new SqlToken(SqlTokenType.LessThan, "<", line, column);
            case '>':
                Advance();
                if (Peek() == '=')
                {
                    Advance();
                    return new SqlToken(SqlTokenType.GreaterEqual, ">=", line, column);
                }

                if (Peek() == '>')
                {
                    Advance();
                    return new SqlToken(SqlTokenType.RightShift, ">>", line, column);
                }

                return new SqlToken(SqlTokenType.GreaterThan, ">", line, column);
            case '!':
                Advance();
                if (Peek() == '=')
                {
                    Advance();
                    return new SqlToken(SqlTokenType.NotEqual, "!=", line, column);
                }

                _diagnostics?.AddError(string.Empty, default,
                    "SQL001", $"意外的字符 '!'");
                return new SqlToken(SqlTokenType.Invalid, "!", line, column);
            case '\'':
                return ScanString(line, column);
            case '"':
                return ScanQuotedIdentifier(line, column);
            case '`':
                return ScanBacktickIdentifier(line, column);
            default:
            {
                if (c == '-' || char.IsDigit(c)) return ScanNumber(line, column);

                if (c == '_' || char.IsLetter(c)) return ScanIdentifierOrKeyword(line, column);

                _diagnostics?.AddError(string.Empty, default,
                    "SQL002", $"意外的字符 '{c}'");
                Advance();
                return new SqlToken(SqlTokenType.Invalid, c.ToString(), line, column);
            }
        }
    }

    private SqlToken ScanString(int line, int column)
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '\'')
        {
            if (Peek() == '\'' && PeekNext() == '\'')
            {
                sb.Append('\'');
                Advance();
                Advance();
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd()) Advance();

        return new SqlToken(SqlTokenType.String, sb.ToString(), line, column);
    }

    private SqlToken ScanQuotedIdentifier(int line, int column)
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"') sb.Append(Advance());

        if (!IsAtEnd()) Advance();

        return new SqlToken(SqlTokenType.Identifier, sb.ToString(), line, column);
    }

    private SqlToken ScanBacktickIdentifier(int line, int column)
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '`') sb.Append(Advance());

        if (!IsAtEnd()) Advance();

        return new SqlToken(SqlTokenType.Identifier, sb.ToString(), line, column);
    }

    private SqlToken ScanNumber(int line, int column)
    {
        var start = _position;

        if (Peek() == '-') Advance();

        while (!IsAtEnd() && char.IsDigit(Peek())) Advance();

        if (!IsAtEnd() && Peek() == '.')
        {
            Advance();
            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            Advance();
            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) Advance();
            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        var text = _source[start.._position];
        return new SqlToken(SqlTokenType.Number, text, line, column);
    }

    private SqlToken ScanIdentifierOrKeyword(int line, int column)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_')) sb.Append(Advance());

        var text = sb.ToString();

        if (Keywords.TryGetValue(text, out var keywordType))
            return new SqlToken(keywordType, text, line, column);

        return new SqlToken(SqlTokenType.Identifier, text, line, column);
    }
}
