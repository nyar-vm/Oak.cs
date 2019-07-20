namespace Oak.Valkyrie.Query;

public sealed class DeleteQueryAst : QueryAstNode
{
    public override string NodeKind => "delete";
    public string TargetTypeName { get; }
    public QueryPredicateAst Predicate { get; }

    public DeleteQueryAst(string targetTypeName, QueryPredicateAst predicate)
    {
        TargetTypeName = targetTypeName;
        Predicate = predicate;
    }
}