namespace Oak.Valkyrie.Query;

public sealed class NotPredicateAst : QueryPredicateAst
{
    public override string PredicateKind => "not";
    public QueryPredicateAst Inner { get; }

    public NotPredicateAst(QueryPredicateAst inner)
    {
        Inner = inner;
    }
}