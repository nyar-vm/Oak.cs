namespace Oak.Valkyrie.Query;

public sealed class FindQueryAst : QueryAstNode
{
    public override string NodeKind => "find";
    public string TargetTypeName { get; }
    public QueryPredicateAst? Predicate { get; }
    public QueryOrderingAst? Ordering { get; }
    public QueryPaginationAst? Pagination { get; }

    public FindQueryAst(string targetTypeName, QueryPredicateAst? predicate = null, QueryOrderingAst? ordering = null, QueryPaginationAst? pagination = null)
    {
        TargetTypeName = targetTypeName;
        Predicate = predicate;
        Ordering = ordering;
        Pagination = pagination;
    }
}