namespace Oak.Valkyrie.AST.Type;

/// <summary>
/// T?
/// +T
/// -T
/// </summary>
public sealed record TypeUnaryExpression : TypeNode
{
    public string Operator { get; init; } = string.Empty;

    public TypeNode Operand { get; init; } = new();

    public bool IsPrefix { get; init; }

    public TypeUnaryExpression() { }

    public TypeUnaryExpression(string op, TypeNode operand, bool isPrefix = false)
    {
        Operator = op;
        Operand = operand;
        IsPrefix = isPrefix;
    }
}