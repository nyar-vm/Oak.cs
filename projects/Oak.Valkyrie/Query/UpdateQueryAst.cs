namespace Oak.Valkyrie.Query;

public sealed class UpdateQueryAst : QueryAstNode
{
    public override string NodeKind => "update";
    public string TargetTypeName { get; }
    public QueryPredicateAst Predicate { get; }
    public IReadOnlyList<FieldAssignmentAst> Assignments { get; }

    public UpdateQueryAst(string targetTypeName, QueryPredicateAst predicate, IReadOnlyList<FieldAssignmentAst> assignments)
    {
        TargetTypeName = targetTypeName;
        Predicate = predicate;
        Assignments = assignments;
    }
}