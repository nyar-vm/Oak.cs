namespace Oak.Valkyrie.AST.Neural;

/// <summary>
///     简单 Neural 层声明（如池化层、归一化层等）
/// </summary>
public sealed record SimpleLayerDecl : NeuralLayerDecl
{
    /// <summary>
    ///     层的种类名称
    /// </summary>
    public string LayerKind { get; init; } = string.Empty;

    /// <summary>
    ///     层参数列表
    /// </summary>
    public IReadOnlyList<NeuralLayerParamDecl> Parameters { get; init; } = [];
}
