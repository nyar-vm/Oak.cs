namespace Oak.Valkyrie.Query;

public sealed class QueryPaginationAst
{
    public int Offset { get; }
    public int Limit { get; }

    public QueryPaginationAst(int offset = 0, int limit = 100)
    {
        Offset = offset;
        Limit = limit;
    }
}