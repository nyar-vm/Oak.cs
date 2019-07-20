namespace Oak.Valkyrie.AST.Neural;

/// <summary>
///     Neural 训练配置声明，用于控制模型的训练过程
/// </summary>
/// <para>示例：</para>
/// <code>
/// training {
///     optimizer = "adam";
///     loss = "cross_entropy";
///     epochs = 10;
///     batch_size = 32;
///     learning_rate = 0.001;
///     scheduler = "cosine";
///     precision = "fp16";
///     checkpoints = ["best", "latest"];
/// }
/// </code>
public sealed record NeuralTrainingConfigDecl : ValkyrieNode
{
    /// <summary>
    ///     优化器名称（如 <c>"adam"</c>、<c>"sgd"</c>）
    /// </summary>
    public string Optimizer { get; init; } = string.Empty;

    /// <summary>
    ///     损失函数名称（如 <c>"cross_entropy"</c>、<c>"mse"</c>）
    /// </summary>
    public string Loss { get; init; } = string.Empty;

    /// <summary>
    ///     训练模式（如 <c>"lora"</c>、<c>"full"</c>）
    /// </summary>
    public string? Mode { get; init; }

    /// <summary>
    ///     LoRA 秩（仅 LoRA 训练模式下有效）
    /// </summary>
    public int? LoraRank { get; init; }

    /// <summary>
    ///     LoRA Alpha 缩放参数
    /// </summary>
    public int? LoraAlpha { get; init; }

    /// <summary>
    ///     LoRA 目标层名称列表
    /// </summary>
    public IReadOnlyList<string> LoraTargets { get; init; } = [];

    /// <summary>
    ///     训练轮数
    /// </summary>
    public int? Epochs { get; init; }

    /// <summary>
    ///     批处理大小
    /// </summary>
    public int? BatchSize { get; init; }

    /// <summary>
    ///     学习率调度器名称（如 <c>"cosine"</c>、<c>"linear"</c>）
    /// </summary>
    public string? Scheduler { get; init; }

    /// <summary>
    ///     梯度累积步数
    /// </summary>
    public int? GradientAccumulation { get; init; }

    /// <summary>
    ///     计算精度（如 <c>"fp16"</c>、<c>"bf16"</c>、<c>"fp32"</c>）
    /// </summary>
    public string? Precision { get; init; }

    /// <summary>
    ///     检查点保存策略列表
    /// </summary>
    public IReadOnlyList<string> Checkpoints { get; init; } = [];

    /// <summary>
    ///     额外配置条目（用于未来扩展）
    /// </summary>
    public IReadOnlyList<ConfigEntry> Entries { get; init; } = [];
}
