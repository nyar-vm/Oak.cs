namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     限定路径表达式，表示带模块路径的完整名称引用
/// </summary>
/// <para>示例：</para>
/// <code>
/// Game.Core.Vector3         // Segments = ["Game", "Core", "Vector3"]
/// ::Game.Core.normalize     // IsGlobal = true, Segments = ["Game", "Core", "normalize"]
/// </code>
public sealed record QualifiedPath : ValkyrieNode
{
    /// <summary>
    ///     是否为全局限定路径（以 <c>::</c> 开头）
    /// </summary>
    public bool IsGlobal { get; init; }

    /// <summary>
    ///     是否为包限定路径
    /// </summary>
    public bool IsPackageQualified { get; init; }

    /// <summary>
    ///     路径段列表
    /// </summary>
    public IReadOnlyList<string> Segments { get; init; } = [];

    /// <summary>
    ///     完整路径名称（段之间用 <c>.</c> 连接）
    /// </summary>
    public string FullName => string.Join(".", Segments);
}
