namespace Oak.Obj;

/// <summary>
///     OBJ 网格包围盒
/// </summary>
public sealed class ObjBounds
{
    /// <summary>
    ///     中心点 [x, y, z]
    /// </summary>
    public float[] Center { get; init; } = [0, 0, 0];

    /// <summary>
    ///     扩展范围 [x, y, z]
    /// </summary>
    public float[] Extents { get; init; } = [0, 0, 0];
}