namespace Oak.Valkyrie.Query;

public sealed class AggregateOperationAst
{
    public AggregateFunctionAst Function { get; }
    public string FieldName { get; }
    public string? Alias { get; }

    public AggregateOperationAst(AggregateFunctionAst function, string fieldName, string? alias = null)
    {
        Function = function;
        FieldName = fieldName;
        Alias = alias;
    }
}