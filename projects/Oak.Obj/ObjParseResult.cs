namespace Oak.Obj;

/// <summary>
///     OBJ 解析结果
/// </summary>
public sealed class ObjParseResult
{
    /// <summary>
    ///     顶点列表
    /// </summary>
    public List<ObjVertex> Vertices { get; init; } = [];

    /// <summary>
    ///     索引列表
    /// </summary>
    public List<int> Indices { get; init; } = [];

    /// <summary>
    ///     子网格列表
    /// </summary>
    public List<ObjSubMesh> SubMeshes { get; init; } = [];

    /// <summary>
    ///     子网格名称列表
    /// </summary>
    public List<string> SubMeshNames { get; init; } = [];

    /// <summary>
    ///     包围盒
    /// </summary>
    public ObjBounds Bounds { get; init; } = new();
}