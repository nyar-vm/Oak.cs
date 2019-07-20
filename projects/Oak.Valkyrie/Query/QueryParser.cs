namespace Oak.Valkyrie.Query;

public sealed class QueryParser
{
    private IReadOnlyList<QueryToken> _tokens = [];
    private int _current;

    public QueryAstNode Parse(string source)
    {
        var lexer = new QueryLexer();
        _tokens = lexer.Tokenize(source);
        _current = 0;

        return ParseQuery();
    }

    private QueryAstNode ParseQuery()
    {
        if (IsAtEnd())
        {
            throw new QueryParseException(0, "查询不能为空");
        }

        return Current().Type switch
        {
            QueryTokenType.KeywordFind => ParseFind(),
            QueryTokenType.KeywordCreate => ParseCreate(),
            QueryTokenType.KeywordUpdate => ParseUpdate(),
            QueryTokenType.KeywordDelete => ParseDelete(),
            QueryTokenType.KeywordAggregate => ParseAggregate(),
            _ => throw new QueryParseException(Current().Position, $"期望查询关键字（find/create/update/delete/aggregate），得到 {Current().Value}")
        };
    }

    private FindQueryAst ParseFind()
    {
        Advance();
        var targetTypeName = ExpectIdentifier();

        QueryPredicateAst? predicate = null;
        if (Match(QueryTokenType.KeywordWhere))
        {
            predicate = ParsePredicate();
        }

        QueryOrderingAst? ordering = null;
        if (Match(QueryTokenType.KeywordOrderBy))
        {
            var fieldName = ExpectIdentifier();
            var descending = false;
            if (Match(QueryTokenType.KeywordDesc))
            {
                descending = true;
            }
            else
            {
                Match(QueryTokenType.KeywordAsc);
            }
            ordering = new QueryOrderingAst(fieldName, descending);
        }

        QueryPaginationAst? pagination = null;
        if (Match(QueryTokenType.KeywordSkip))
        {
            var offset = ExpectNumber();
            var limit = 100;
            if (Match(QueryTokenType.KeywordTake))
            {
                limit = ExpectNumber();
            }
            pagination = new QueryPaginationAst(offset, limit);
        }
        else if (Match(QueryTokenType.KeywordTake))
        {
            var limit = ExpectNumber();
            pagination = new QueryPaginationAst(0, limit);
        }

        return new FindQueryAst(targetTypeName, predicate, ordering, pagination);
    }

    private CreateQueryAst ParseCreate()
    {
        Advance();
        var targetTypeName = ExpectIdentifier();
        Expect(QueryTokenType.LeftBrace);
        var assignments = ParseAssignments();
        Expect(QueryTokenType.RightBrace);

        return new CreateQueryAst(targetTypeName, assignments);
    }

    private UpdateQueryAst ParseUpdate()
    {
        Advance();
        var targetTypeName = ExpectIdentifier();
        Expect(QueryTokenType.KeywordWhere);
        var predicate = ParsePredicate();
        Expect(QueryTokenType.KeywordSet);
        Expect(QueryTokenType.LeftBrace);
        var assignments = ParseAssignments();
        Expect(QueryTokenType.RightBrace);

        return new UpdateQueryAst(targetTypeName, predicate, assignments);
    }

    private DeleteQueryAst ParseDelete()
    {
        Advance();
        var targetTypeName = ExpectIdentifier();
        Expect(QueryTokenType.KeywordWhere);
        var predicate = ParsePredicate();

        return new DeleteQueryAst(targetTypeName, predicate);
    }

    private AggregateQueryAst ParseAggregate()
    {
        Advance();
        var sourceTypeName = ExpectIdentifier();

        QueryPredicateAst? predicate = null;
        if (Match(QueryTokenType.KeywordWhere))
        {
            predicate = ParsePredicate();
        }

        Expect(QueryTokenType.LeftBrace);
        var operations = ParseAggregateOperations();
        Expect(QueryTokenType.RightBrace);

        List<string> groupBy = [];
        if (Match(QueryTokenType.KeywordGroupBy))
        {
            Expect(QueryTokenType.KeywordOrderBy);
            groupBy.Add(ExpectIdentifier());
            while (Match(QueryTokenType.Comma))
            {
                groupBy.Add(ExpectIdentifier());
            }
        }

        return new AggregateQueryAst(sourceTypeName, operations, predicate, groupBy);
    }

    private QueryPredicateAst ParsePredicate()
    {
        return ParseOrPredicate();
    }

    private QueryPredicateAst ParseOrPredicate()
    {
        var left = ParseAndPredicate();
        while (Match(QueryTokenType.Or))
        {
            var right = ParseAndPredicate();
            left = new OrPredicateAst(left, right);
        }
        return left;
    }

    private QueryPredicateAst ParseAndPredicate()
    {
        var left = ParseNotPredicate();
        while (Match(QueryTokenType.And) || Match(QueryTokenType.Comma))
        {
            var right = ParseNotPredicate();
            left = new AndPredicateAst(left, right);
        }
        return left;
    }

    private QueryPredicateAst ParseNotPredicate()
    {
        if (Match(QueryTokenType.Not))
        {
            return new NotPredicateAst(ParsePrimaryPredicate());
        }
        return ParsePrimaryPredicate();
    }

    private QueryPredicateAst ParsePrimaryPredicate()
    {
        if (Match(QueryTokenType.LeftParen))
        {
            var inner = ParsePredicate();
            Expect(QueryTokenType.RightParen);
            return inner;
        }

        var fieldName = ExpectIdentifier();

        if (Match(QueryTokenType.Equals))
        {
            return new FieldEqualsAst(fieldName, ParseValue());
        }

        if (Match(QueryTokenType.NotEquals))
        {
            var value = ParseValue();
            return new NotPredicateAst(new FieldEqualsAst(fieldName, value));
        }

        if (Match(QueryTokenType.GreaterThan))
        {
            return new FieldGreaterThanAst(fieldName, ParseValue());
        }

        if (Match(QueryTokenType.LessThan))
        {
            return new FieldLessThanAst(fieldName, ParseValue());
        }

        if (Match(QueryTokenType.GreaterEqual))
        {
            return new NotPredicateAst(new FieldLessThanAst(fieldName, ParseValue()));
        }

        if (Match(QueryTokenType.LessEqual))
        {
            return new NotPredicateAst(new FieldGreaterThanAst(fieldName, ParseValue()));
        }

        if (Match(QueryTokenType.KeywordContains))
        {
            return new FieldContainsAst(fieldName, ParseValue());
        }

        if (Match(QueryTokenType.KeywordIn))
        {
            Expect(QueryTokenType.LeftBracket);
            var values = new List<object> { ParseValue() };
            while (Match(QueryTokenType.Comma))
            {
                values.Add(ParseValue());
            }
            Expect(QueryTokenType.RightBracket);
            return new FieldInAst(fieldName, values);
        }

        throw new QueryParseException(Current().Position, $"字段 '{fieldName}' 后期望比较运算符，得到 {Current().Value}");
    }

    private object ParseValue()
    {
        if (Current().Type == QueryTokenType.String)
        {
            var val = Current().Value;
            Advance();
            return val;
        }

        if (Current().Type == QueryTokenType.Number)
        {
            var val = Current().Value;
            Advance();
            if (val.Contains('.'))
            {
                return double.Parse(val);
            }
            return int.Parse(val);
        }

        if (Current().Type == QueryTokenType.Identifier)
        {
            var val = Current().Value;
            if (val.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                return true;
            }
            if (val.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                return false;
            }
            if (val.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                return null!;
            }
            Advance();
            return val;
        }

        throw new QueryParseException(Current().Position, $"期望值，得到 {Current().Value}");
    }

    private List<FieldAssignmentAst> ParseAssignments()
    {
        var assignments = new List<FieldAssignmentAst> { ParseAssignment() };
        while (Match(QueryTokenType.Comma))
        {
            assignments.Add(ParseAssignment());
        }
        return assignments;
    }

    private FieldAssignmentAst ParseAssignment()
    {
        var fieldName = ExpectIdentifier();
        Expect(QueryTokenType.Equals);
        var value = ParseValue();
        return new FieldAssignmentAst(fieldName, value);
    }

    private List<AggregateOperationAst> ParseAggregateOperations()
    {
        var operations = new List<AggregateOperationAst> { ParseAggregateOperation() };
        while (Match(QueryTokenType.Comma))
        {
            operations.Add(ParseAggregateOperation());
        }
        return operations;
    }

    private AggregateOperationAst ParseAggregateOperation()
    {
        var funcName = ExpectIdentifier().ToLowerInvariant();
        var function = funcName switch
        {
            "count" => AggregateFunctionAst.Count,
            "sum" => AggregateFunctionAst.Sum,
            "avg" => AggregateFunctionAst.Avg,
            "min" => AggregateFunctionAst.Min,
            "max" => AggregateFunctionAst.Max,
            "first" => AggregateFunctionAst.First,
            "last" => AggregateFunctionAst.Last,
            "distinct" => AggregateFunctionAst.Distinct,
            _ => throw new QueryParseException(Current().Position, $"未知的聚合函数: {funcName}")
        };

        Expect(QueryTokenType.LeftParen);
        var fieldName = ExpectIdentifier();
        Expect(QueryTokenType.RightParen);

        string? alias = null;
        if (Match(QueryTokenType.KeywordAs))
        {
            alias = ExpectIdentifier();
        }

        return new AggregateOperationAst(function, fieldName, alias);
    }

    private QueryToken Current() => _current < _tokens.Count ? _tokens[_current] : _tokens[^1];

    private bool IsAtEnd() => Current().Type == QueryTokenType.Eof;

    private bool Match(QueryTokenType type)
    {
        if (Current().Type == type)
        {
            Advance();
            return true;
        }
        return false;
    }

    private QueryToken Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }
        return _tokens[_current - 1];
    }

    private QueryToken Expect(QueryTokenType type)
    {
        if (Current().Type != type)
        {
            throw new QueryParseException(Current().Position, $"期望 {type}，得到 {Current().Type}({Current().Value})");
        }
        return Advance();
    }

    private string ExpectIdentifier()
    {
        if (Current().Type != QueryTokenType.Identifier)
        {
            throw new QueryParseException(Current().Position, $"期望标识符，得到 {Current().Type}({Current().Value})");
        }
        var val = Current().Value;
        Advance();
        return val;
    }

    private int ExpectNumber()
    {
        if (Current().Type != QueryTokenType.Number)
        {
            throw new QueryParseException(Current().Position, $"期望数字，得到 {Current().Type}({Current().Value})");
        }
        var val = int.Parse(Current().Value);
        Advance();
        return val;
    }
}