namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
/// f(x, y, z)
/// </summary>
public sealed record ArgumentList
{
    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<ArgumentItem> Items { get; init; } = [];
}
