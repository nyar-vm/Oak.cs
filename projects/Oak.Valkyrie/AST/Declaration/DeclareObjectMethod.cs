using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Declaration;

public sealed record DeclareObjectMethod : ValkyrieNode, IDeclarationNode
{
    public Annotations Annotations { get; init; } = new();
    public IdentifierNode? Name { get; init; } = new();
}
