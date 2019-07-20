namespace Oak.Valkyrie.AST.Type;

/// <summary>
/// T | U
/// T &amp; U
/// T -> U
/// </summary>
public record TypeBinaryExpression : TypeNode
{
    public string Operator { get; init; } = string.Empty;

    public TypeNode Left { get; init; } = new();

    public TypeNode Right { get; init; } = new();

    public TypeBinaryExpression() { }

    public TypeBinaryExpression(string op, TypeNode left, TypeNode right)
    {
        Operator = op;
        Left = left;
        Right = right;
    }
}