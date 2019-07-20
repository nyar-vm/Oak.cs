namespace Oak.Valkyrie.AST.Neural;

/// <summary>
///     卷积（Conv）Neural 层声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// layer conv1 = Conv2D { input_channels = 3, output_channels = 32, kernel_size = 3 };
/// </code>
public sealed record ConvLayerDecl : NeuralLayerDecl
{
    /// <summary>
    ///     输入通道数
    /// </summary>
    public int InputChannels { get; init; }

    /// <summary>
    ///     输出通道数（卷积核数量）
    /// </summary>
    public int OutputChannels { get; init; }

    /// <summary>
    ///     卷积核大小
    /// </summary>
    public int KernelSize { get; init; }
}
