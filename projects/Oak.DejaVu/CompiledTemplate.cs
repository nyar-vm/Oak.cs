using Oak.DejaVu.Optimizer;

namespace Oak.DejaVu;

/// <summary>
///     编译后的模板产物——可缓存的优化 AST
/// </summary>
public sealed class CompiledTemplate
{
    /// <summary>
    ///     模板源路径
    /// </summary>
    public string TemplatePath { get; init; } = string.Empty;

    /// <summary>
    ///     优化后的模板节点
    /// </summary>
    public List<DejaVuTemplateNode> Nodes { get; init; } = [];

    /// <summary>
    ///     编译时间戳
    /// </summary>
    public DateTimeOffset CompiledAt { get; init; }

    /// <summary>
    ///     源文件最后写入时间（用于缓存失效检测）
    /// </summary>
    public DateTimeOffset SourceLastWriteTime { get; init; }

    /// <summary>
    ///     编译期符号表（变量作用域、block 名称、include 路径等）
    ///     ，按需生成，为 null 时表示未执行符号解析。
    /// </summary>
    public SymbolTable? SymbolTable { get; init; }

    /// <summary>
    ///     编译后的渲染委托（AST → 表达式树 → JIT 编译）。
    ///     为 null 时表示尚未生成渲染委托。
    /// </summary>
    public Func<IDictionary<string, object>, string>? RenderFunc { get; init; }

    /// <summary>
    ///     从解析结果编译模板
    /// </summary>
    public static CompiledTemplate Compile(DejaVuParseResult parseResult, string templatePath, DateTimeOffset sourceLastWriteTime)
    {
        var optimizer = new TemplateOptimizer();
        var optimizedNodes = optimizer.Optimize(parseResult.Nodes.ToList());

        return new CompiledTemplate
        {
            TemplatePath = templatePath,
            Nodes = optimizedNodes,
            CompiledAt = DateTimeOffset.UtcNow,
            SourceLastWriteTime = sourceLastWriteTime
        };
    }
}
