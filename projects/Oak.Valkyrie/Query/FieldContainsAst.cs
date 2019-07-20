namespace Oak.Valkyrie.Query;

public sealed class FieldContainsAst : QueryPredicateAst
{
    public override string PredicateKind => "contains";
    public string FieldName { get; }
    public object Value { get; }

    public FieldContainsAst(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }
}