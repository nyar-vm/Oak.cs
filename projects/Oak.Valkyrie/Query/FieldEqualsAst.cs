namespace Oak.Valkyrie.Query;

public sealed class FieldEqualsAst : QueryPredicateAst
{
    public override string PredicateKind => "equals";
    public string FieldName { get; }
    public object Value { get; }

    public FieldEqualsAst(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }
}