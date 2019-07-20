namespace Oak.Obj;

/// <summary>
///     OBJ 子网格
/// </summary>
public sealed class ObjSubMesh
{
    /// <summary>
    ///     索引起始位置
    /// </summary>
    public int IndexStart { get; init; }

    /// <summary>
    ///     索引数量
    /// </summary>
    public int IndexCount { get; init; }

    /// <summary>
    ///     材质路径
    /// </summary>
    public string MaterialPath { get; init; } = string.Empty;
}