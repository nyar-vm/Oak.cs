using Oak.Valkyrie.AST.Declaration;

namespace Oak.Valkyrie.AST.Neural;

/// <summary>
///     Neural 网络声明，定义神经网络的结构、输入输出和训练/推理配置
/// </summary>
/// <para>示例：</para>
/// <code>
/// neural MNIST {
///     inputs {
///         var image: Tensor[f32; 28, 28, 1];
///     }
///     outputs {
///         var label: Tensor[f32; 10];
///     }
///
///     layer dense1 = Dense { input = 784, output = 256, activation = "relu" };
///     layer dense2 = Dense { input = 256, output = 10, activation = "softmax" };
///
///     training {
///         optimizer = "adam";
///         loss = "cross_entropy";
///         epochs = 10;
///         batch_size = 32;
///     }
///
///     inference {
///         kv_cache = true;
///         kv_cache_dtype = "fp16";
///     }
/// }
/// </code>
public sealed record NeuralDecl : ValkyrieNode
{
    /// <summary>
    ///     Neural 网络名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes { get; init; } = [];

    /// <summary>
    ///     输入张量定义列表
    /// </summary>
    public IReadOnlyList<DeclareObjectField> Inputs { get; init; } = [];

    /// <summary>
    ///     输出张量定义列表
    /// </summary>
    public IReadOnlyList<DeclareObjectField> Outputs { get; init; } = [];

    /// <summary>
    ///     可训练参数列表
    /// </summary>
    public IReadOnlyList<DeclareObjectField> Parameters { get; init; } = [];

    /// <summary>
    ///     网络层列表
    /// </summary>
    public IReadOnlyList<NeuralLayerDecl> Layers { get; init; } = [];

    /// <summary>
    ///     推理配置，为 <c>null</c> 时使用默认推理配置
    /// </summary>
    public NeuralInferenceConfigDecl? InferenceConfig { get; init; }

    /// <summary>
    ///     训练配置，为 <c>null</c> 时不可训练
    /// </summary>
    public NeuralTrainingConfigDecl? TrainingConfig { get; init; }
}
