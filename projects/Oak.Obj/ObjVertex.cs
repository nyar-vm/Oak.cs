namespace Oak.Obj;

/// <summary>
///     OBJ 网格顶点
/// </summary>
public sealed class ObjVertex
{
    /// <summary>
    ///     位置坐标 [x, y, z]
    /// </summary>
    public float[] Position { get; init; } = [0, 0, 0];

    /// <summary>
    ///     法线坐标 [nx, ny, nz]
    /// </summary>
    public float[]? Normal { get; init; }

    /// <summary>
    ///     纹理坐标 [u, v]
    /// </summary>
    public float[]? Uv { get; init; }
}