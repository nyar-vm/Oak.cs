namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     Using 导入声明，引入外部模块或命名空间
/// </summary>
/// <para>支持多种导入形式：</para>
/// <code>
/// using std.math;                  # 导入整个模块
/// using std.math.{sin, cos};       # 选择性导入
/// using std.collections.HashMap as Map;   # 带别名导入
/// </code>
public sealed record DeclareUsing : ValkyrieNode
{
    /// <summary>
    ///     导入的模块路径
    /// </summary>
    public string ModulePath { get; init; } = string.Empty;

    /// <summary>
    ///     目标命名空间（当从模块中导入特定命名空间时）
    /// </summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>
    ///     模块别名，为 <c>null</c> 时使用原名
    /// </summary>
    public string? Alias { get; init; }

    /// <summary>
    ///     选择性导入的名称列表（<c>{ a, b } from module</c> 形式）
    /// </summary>
    public IReadOnlyList<string> Selections { get; init; } = [];

    /// <summary>
    ///     类型别名列表（<c>as Alias</c> 形式）
    /// </summary>
    public IReadOnlyList<UsingAliasEntry> TypeAliases { get; init; } = [];
}
