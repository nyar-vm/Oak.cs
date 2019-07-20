namespace Oak.Valkyrie.Query;

public sealed class CreateQueryAst : QueryAstNode
{
    public override string NodeKind => "create";
    public string TargetTypeName { get; }
    public IReadOnlyList<FieldAssignmentAst> Assignments { get; }

    public CreateQueryAst(string targetTypeName, IReadOnlyList<FieldAssignmentAst> assignments)
    {
        TargetTypeName = targetTypeName;
        Assignments = assignments;
    }
}