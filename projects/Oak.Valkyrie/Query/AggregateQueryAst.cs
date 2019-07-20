namespace Oak.Valkyrie.Query;

public sealed class AggregateQueryAst : QueryAstNode
{
    public override string NodeKind => "aggregate";
    public string SourceTypeName { get; }
    public IReadOnlyList<AggregateOperationAst> Operations { get; }
    public QueryPredicateAst? Predicate { get; }
    public IReadOnlyList<string> GroupBy { get; }

    public AggregateQueryAst(string sourceTypeName, IReadOnlyList<AggregateOperationAst> operations, QueryPredicateAst? predicate = null, IReadOnlyList<string>? groupBy = null)
    {
        SourceTypeName = sourceTypeName;
        Operations = operations;
        Predicate = predicate;
        GroupBy = groupBy ?? [];
    }
}