namespace Oak.Valkyrie.Query;

public sealed class FieldInAst : QueryPredicateAst
{
    public override string PredicateKind => "in";
    public string FieldName { get; }
    public IReadOnlyList<object> Values { get; }

    public FieldInAst(string fieldName, IReadOnlyList<object> values)
    {
        FieldName = fieldName;
        Values = values;
    }
}