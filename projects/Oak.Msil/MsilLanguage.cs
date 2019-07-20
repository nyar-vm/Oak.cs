using Oak.Syntax;

namespace Oak.Msil;

/// <summary>
///     MSIL 字节码文本语言定义
///     MSIL 是 .NET CLR 的标准中间语言文本表示
///     参考：https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit.ilgenerator
/// </summary>
public sealed class MsilLanguage : Language
{
    /// <summary>
    ///     语言名称
    /// </summary>
    public override string Name => "MSIL";

    /// <summary>
    ///     默认实例
    /// </summary>
    public static MsilLanguage Default => new();
}
