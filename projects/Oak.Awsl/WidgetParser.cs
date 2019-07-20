using Oak.Diagnostics;

namespace Oak.Widget;

/// <summary>
///     Widget 组件解析器，基于 AwslParser 进行 Token 流解析。
///     支持 JSX/Svelte 风格的单文件组件，含 AWSL 所有语法特性。
/// </summary>
public sealed partial class WidgetParser
{
    private readonly AwslParser _parser;

    /// <summary>
    ///     创建 Widget 组件解析器
    /// </summary>
    /// <param name="diagnostics">诊断接收器</param>
    public WidgetParser(DiagnosticSink? diagnostics = null)
    {
        _parser = new AwslParser(diagnostics);
    }

    /// <summary>
    ///     解析 Widget 组件源码
    /// </summary>
    /// <param name="source">AWSL 源码</param>
    /// <param name="filePath">源文件路径（用于提取组件名）</param>
    /// <returns>解析结果</returns>
    public WidgetParseResult Parse(string source, string filePath = "")
    {
        return _parser.Parse(source, filePath);
    }
}
