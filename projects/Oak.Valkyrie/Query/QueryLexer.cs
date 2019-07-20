using System.Text;

namespace Oak.Valkyrie.Query;

public sealed class QueryLexer
{
    private static readonly Dictionary<string, QueryTokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["find"] = QueryTokenType.KeywordFind,
        ["create"] = QueryTokenType.KeywordCreate,
        ["update"] = QueryTokenType.KeywordUpdate,
        ["delete"] = QueryTokenType.KeywordDelete,
        ["aggregate"] = QueryTokenType.KeywordAggregate,
        ["where"] = QueryTokenType.KeywordWhere,
        ["set"] = QueryTokenType.KeywordSet,
        ["order"] = QueryTokenType.KeywordOrderBy,
        ["by"] = QueryTokenType.KeywordOrderBy,
        ["skip"] = QueryTokenType.KeywordSkip,
        ["take"] = QueryTokenType.KeywordTake,
        ["group"] = QueryTokenType.KeywordGroupBy,
        ["as"] = QueryTokenType.KeywordAs,
        ["in"] = QueryTokenType.KeywordIn,
        ["contains"] = QueryTokenType.KeywordContains,
        ["desc"] = QueryTokenType.KeywordDesc,
        ["asc"] = QueryTokenType.KeywordAsc,
        ["and"] = QueryTokenType.And,
        ["or"] = QueryTokenType.Or,
        ["not"] = QueryTokenType.Not
    };

    public IReadOnlyList<QueryToken> Tokenize(string source)
    {
        var tokens = new List<QueryToken>();
        var pos = 0;

        while (pos < source.Length)
        {
            var c = source[pos];

            if (char.IsWhiteSpace(c))
            {
                pos++;
                continue;
            }

            if (c == '/' && pos + 1 < source.Length && source[pos + 1] == '/')
            {
                while (pos < source.Length && source[pos] != '\n')
                {
                    pos++;
                }
                continue;
            }

            if (c == '"')
            {
                var sb = new StringBuilder();
                pos++;
                while (pos < source.Length && source[pos] != '"')
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                    {
                        pos++;
                        sb.Append(source[pos] switch
                        {
                            'n' => '\n',
                            't' => '\t',
                            'r' => '\r',
                            _ => source[pos]
                        });
                    }
                    else
                    {
                        sb.Append(source[pos]);
                    }
                    pos++;
                }
                pos++;
                tokens.Add(new QueryToken(QueryTokenType.String, sb.ToString(), pos));
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                var start = pos;
                if (c == '-') pos++;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.'))
                {
                    pos++;
                }
                tokens.Add(new QueryToken(QueryTokenType.Number, source[start..pos], start));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                {
                    pos++;
                }
                var word = source[start..pos];
                var tokenType = Keywords.TryGetValue(word, out var kwType) ? kwType : QueryTokenType.Identifier;
                tokens.Add(new QueryToken(tokenType, word, start));
                continue;
            }

            var token = c switch
            {
                '(' => new QueryToken(QueryTokenType.LeftParen, "(", pos),
                ')' => new QueryToken(QueryTokenType.RightParen, ")", pos),
                '{' => new QueryToken(QueryTokenType.LeftBrace, "{", pos),
                '}' => new QueryToken(QueryTokenType.RightBrace, "}", pos),
                '[' => new QueryToken(QueryTokenType.LeftBracket, "[", pos),
                ']' => new QueryToken(QueryTokenType.RightBracket, "]", pos),
                ',' => new QueryToken(QueryTokenType.Comma, ",", pos),
                '.' => new QueryToken(QueryTokenType.Dot, ".", pos),
                ';' => new QueryToken(QueryTokenType.Semicolon, ";", pos),
                '=' when pos + 1 < source.Length && source[pos + 1] == '=' => new QueryToken(QueryTokenType.Equals, "==", pos),
                '!' when pos + 1 < source.Length && source[pos + 1] == '=' => new QueryToken(QueryTokenType.NotEquals, "!=", pos),
                '<' when pos + 1 < source.Length && source[pos + 1] == '=' => new QueryToken(QueryTokenType.LessEqual, "<=", pos),
                '>' when pos + 1 < source.Length && source[pos + 1] == '=' => new QueryToken(QueryTokenType.GreaterEqual, ">=", pos),
                '<' => new QueryToken(QueryTokenType.LessThan, "<", pos),
                '>' => new QueryToken(QueryTokenType.GreaterThan, ">", pos),
                '=' => new QueryToken(QueryTokenType.Equals, "=", pos),
                _ => null
            };

            if (token != null)
            {
                pos += token.Value.Length;
                tokens.Add(token);
            }
            else
            {
                pos++;
            }
        }

        tokens.Add(new QueryToken(QueryTokenType.Eof, "", pos));
        return tokens;
    }
}
