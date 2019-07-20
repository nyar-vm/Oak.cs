namespace Oak.Valkyrie.Query;

public sealed class OrPredicateAst : QueryPredicateAst
{
    public override string PredicateKind => "or";
    public QueryPredicateAst Left { get; }
    public QueryPredicateAst Right { get; }

    public OrPredicateAst(QueryPredicateAst left, QueryPredicateAst right)
    {
        Left = left;
        Right = right;
    }
}