using System.Text;

namespace Oak.Valkyrie.Query;

public sealed class QueryFormatter
{
    public string Format(QueryAstNode node)
    {
        var sb = new StringBuilder();

        switch (node)
        {
            case FindQueryAst find:
                FormatFind(find, sb);
                break;
            case CreateQueryAst create:
                FormatCreate(create, sb);
                break;
            case UpdateQueryAst update:
                FormatUpdate(update, sb);
                break;
            case DeleteQueryAst delete:
                FormatDelete(delete, sb);
                break;
            case AggregateQueryAst aggregate:
                FormatAggregate(aggregate, sb);
                break;
            default:
                sb.AppendLine($"// 不支持的查询类型: {node.NodeKind}");
                break;
        }

        return sb.ToString();
    }

    private void FormatFind(FindQueryAst query, StringBuilder sb)
    {
        sb.Append($"find {query.TargetTypeName}");

        if (query.Predicate != null)
        {
            sb.Append($" where {FormatPredicate(query.Predicate)}");
        }

        if (query.Ordering != null)
        {
            var dir = query.Ordering.Descending ? " desc" : "";
            sb.Append($" order by {query.Ordering.FieldName}{dir}");
        }

        if (query.Pagination != null)
        {
            sb.Append($" skip {query.Pagination.Offset} take {query.Pagination.Limit}");
        }
    }

    private void FormatCreate(CreateQueryAst query, StringBuilder sb)
    {
        sb.Append($"create {query.TargetTypeName} {{ ");
        sb.Append(string.Join(", ", query.Assignments.Select(a => $"{a.FieldName} = {FormatValue(a.Value)}")));
        sb.Append(" }");
    }

    private void FormatUpdate(UpdateQueryAst query, StringBuilder sb)
    {
        sb.Append($"update {query.TargetTypeName}");
        sb.Append($" where {FormatPredicate(query.Predicate)}");
        sb.Append(" set { ");
        sb.Append(string.Join(", ", query.Assignments.Select(a => $"{a.FieldName} = {FormatValue(a.Value)}")));
        sb.Append(" }");
    }

    private void FormatDelete(DeleteQueryAst query, StringBuilder sb)
    {
        sb.Append($"delete {query.TargetTypeName}");
        sb.Append($" where {FormatPredicate(query.Predicate)}");
    }

    private void FormatAggregate(AggregateQueryAst query, StringBuilder sb)
    {
        sb.Append($"aggregate {query.SourceTypeName}");

        if (query.Predicate != null)
        {
            sb.Append($" where {FormatPredicate(query.Predicate)}");
        }

        sb.Append(" { ");
        sb.Append(string.Join(", ", query.Operations.Select(FormatAggregateOp)));
        sb.Append(" }");

        if (query.GroupBy.Count > 0)
        {
            sb.Append($" group by {string.Join(", ", query.GroupBy)}");
        }
    }

    private string FormatAggregateOp(AggregateOperationAst op)
    {
        var funcName = op.Function.ToString().ToLowerInvariant();
        var alias = op.Alias != null ? $" as {op.Alias}" : "";
        return $"{funcName}({op.FieldName}){alias}";
    }

    private string FormatPredicate(QueryPredicateAst predicate)
    {
        return predicate switch
        {
            FieldEqualsAst eq => $"{eq.FieldName} == {FormatValue(eq.Value)}",
            FieldGreaterThanAst gt => $"{gt.FieldName} > {FormatValue(gt.Value)}",
            FieldLessThanAst lt => $"{lt.FieldName} < {FormatValue(lt.Value)}",
            FieldContainsAst ct => $"{ct.FieldName} contains {FormatValue(ct.Value)}",
            FieldInAst fin => $"{fin.FieldName} in [{string.Join(", ", fin.Values.Select(FormatValue))}]",
            AndPredicateAst and => $"({FormatPredicate(and.Left)} && {FormatPredicate(and.Right)})",
            OrPredicateAst or => $"({FormatPredicate(or.Left)} || {FormatPredicate(or.Right)})",
            NotPredicateAst not => $"!{FormatPredicate(not.Inner)}",
            _ => $"/* unknown predicate: {predicate.PredicateKind} */"
        };
    }

    private static string FormatValue(object value)
    {
        return value switch
        {
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            null => "null",
            _ => value.ToString() ?? "null"
        };
    }
}
