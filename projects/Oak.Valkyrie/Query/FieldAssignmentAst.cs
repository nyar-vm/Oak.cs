namespace Oak.Valkyrie.Query;

public sealed class FieldAssignmentAst
{
    public string FieldName { get; }
    public object Value { get; }

    public FieldAssignmentAst(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }
}