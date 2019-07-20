namespace Oak.Valkyrie.AST.Type;

/// <summary>
/// let f: T -> U;
/// let f: (T) -> U;
/// let f: (T, ) -> U;
/// let f: micro(T) -> U;
/// </summary>
public record TypeMicroNode : TypeBinaryExpression
{
}