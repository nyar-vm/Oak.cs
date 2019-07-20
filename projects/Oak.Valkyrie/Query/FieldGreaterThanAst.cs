namespace Oak.Valkyrie.Query;

public sealed class FieldGreaterThanAst : QueryPredicateAst
{
    public override string PredicateKind => "greater_than";
    public string FieldName { get; }
    public object Value { get; }

    public FieldGreaterThanAst(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }
}