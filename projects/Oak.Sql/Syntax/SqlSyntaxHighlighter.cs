using Oak.Syntax;

namespace Oak.Sql.Syntax;

/// <summary>
///     SQL 语法高亮器
/// </summary>
public sealed class SqlSyntaxHighlighter
{
    private static readonly HashSet<string> SqlTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "varchar", "text", "boolean", "integer", "float", "double",
        "decimal", "date", "timestamp", "blob", "bigint", "smallint", "char",
        "nvarchar", "uuid",
    };

    /// <summary>
    ///     对 SQL 源码进行语法高亮
    /// </summary>
    public IReadOnlyList<HighlightSpan> Highlight(string source)
    {
        var lexer = new SqlLexer(source);
        var tokens = lexer.Lex();
        var spans = new List<HighlightSpan>(tokens.Count);

        foreach (var token in tokens)
        {
            var kind = token.Kind.Name switch
            {
                "Keyword" => HighlightKind.Keyword,
                "Number" => HighlightKind.Number,
                "String" => HighlightKind.String,
                "Identifier" => SqlTypes.Contains(token.Text) ? HighlightKind.TypeName : HighlightKind.Identifier,
                "Operator" => HighlightKind.Operator,
                "Delimiter" => HighlightKind.Delimiter,
                _ => HighlightKind.Other
            };
            spans.Add(new HighlightSpan { Kind = kind, Offset = token.Span.Start, Length = token.Span.Length });
        }

        return spans;
    }
}
