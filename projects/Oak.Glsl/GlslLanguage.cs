namespace Oak.Glsl;

/// <summary>
///     GLSL 语言配置。
/// </summary>
public sealed class GlslLanguage : Oak.Syntax.Language
{
    /// <summary>
    ///     语言名称。
    /// </summary>
    public override string Name => "GLSL";

    /// <summary>
    ///     GLSL 版本（如 450）。
    /// </summary>
    public int Version { get; init; } = 450;

    /// <summary>
    ///     是否为 ES 配置文件。
    /// </summary>
    public bool IsEsProfile { get; init; }

    /// <summary>
    ///     是否启用计算着色器。
    /// </summary>
    public bool ComputeShader { get; init; } = true;

    /// <summary>
    ///     是否启用光线追踪扩展。
    /// </summary>
    public bool RayTracing { get; init; }
}
