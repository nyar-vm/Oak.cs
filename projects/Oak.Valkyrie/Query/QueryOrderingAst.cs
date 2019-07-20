namespace Oak.Valkyrie.Query;

public sealed class QueryOrderingAst
{
    public string FieldName { get; }
    public bool Descending { get; }

    public QueryOrderingAst(string fieldName, bool descending = false)
    {
        FieldName = fieldName;
        Descending = descending;
    }
}