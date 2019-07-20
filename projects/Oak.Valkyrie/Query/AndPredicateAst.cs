namespace Oak.Valkyrie.Query;

public sealed class AndPredicateAst : QueryPredicateAst
{
    public override string PredicateKind => "and";
    public QueryPredicateAst Left { get; }
    public QueryPredicateAst Right { get; }

    public AndPredicateAst(QueryPredicateAst left, QueryPredicateAst right)
    {
        Left = left;
        Right = right;
    }
}