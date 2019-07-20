namespace Oak.Valkyrie.Query;

public sealed class FieldLessThanAst : QueryPredicateAst
{
    public override string PredicateKind => "less_than";
    public string FieldName { get; }
    public object Value { get; }

    public FieldLessThanAst(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }
}