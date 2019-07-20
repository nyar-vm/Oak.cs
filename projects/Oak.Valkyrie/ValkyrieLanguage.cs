using Oak.Syntax;

namespace Oak.Valkyrie;

/// <summary>
///     Valkyrie 语言配置
/// </summary>
public sealed class ValkyrieLanguage : Language
{
    public ValkyrieLanguage()
    {
        Name = "Valkyrie";
    }

    private ValkyrieLanguage(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     语言名称
    /// </summary>
    public override string Name { get; }

    /// <summary>
    ///     文件扩展名列表
    /// </summary>
    public IReadOnlyList<string> Extensions { get; init; } = [".script"];

    /// <summary>
    ///     是否支持 Schema 扩展（Hermes 模式）
    ///     启用后支持 storage, service, rpc 等声明
    /// </summary>
    public bool SupportSchemaExtension { get; init; }

    /// <summary>
    ///     是否支持 ECS 扩展
    ///     启用后支持 entity, component, system 等声明
    /// </summary>
    public bool SupportEcsExtension { get; init; } = true;

    /// <summary>
    ///     是否支持 UI 扩展
    /// </summary>
    public bool SupportUiExtension { get; init; } = true;

    /// <summary>
    ///     是否支持 Shader 扩展
    ///     启用后支持 shader, uniform, varying, texture, sampler 等着色器语句
    /// </summary>
    public bool SupportShaderExtension { get; init; }

    /// <summary>
    ///     标准 Valkyrie 语言（游戏脚本模式）
    /// </summary>
    public static ValkyrieLanguage Standard => new("Valkyrie")
    {
        Extensions = [".script"],
        SupportSchemaExtension = false,
        SupportEcsExtension = true,
        SupportUiExtension = true,
        SupportShaderExtension = false
    };

    /// <summary>
    ///     Editor UI 模式（Widget SFC 模式）
    /// </summary>
    public static ValkyrieLanguage EditorUI => new("Valkyrie Editor UI")
    {
        Extensions = [".widget"],
        SupportSchemaExtension = false,
        SupportEcsExtension = false,
        SupportUiExtension = true,
        SupportShaderExtension = false
    };

    /// <summary>
    ///     Schema 模式（数据契约模式）
    /// </summary>
    public static ValkyrieLanguage Schema => new("Valkyrie Schema")
    {
        Extensions = [".schema"],
        SupportSchemaExtension = true,
        SupportEcsExtension = false,
        SupportUiExtension = false,
        SupportShaderExtension = false
    };

    /// <summary>
    ///     Shader 模式（着色器语言模式）
    /// </summary>
    public static ValkyrieLanguage Shader => new("Valkyrie Shader")
    {
        Extensions = [".shader"],
        SupportSchemaExtension = false,
        SupportEcsExtension = false,
        SupportUiExtension = false,
        SupportShaderExtension = true
    };
}
