using Oak.DejaVu.CodeGen;
using Oak.DejaVu.Filters;
using Oak.DejaVu.Optimizer;
using Oak.Diagnostics;

namespace Oak.DejaVu;

/// <summary>
///     DejaVu 模板编译器——协调解析、优化、符号解析、错误收集的编译管线。
/// </summary>
public sealed class DejaVuCompiler
{
    private readonly DejaVuParser _parser;
    private readonly TemplateOptimizer _optimizer;
    private readonly SymbolResolver _symbolResolver;

    /// <summary>
    ///     创建模板编译器
    /// </summary>
    /// <param name="parser">模板解析器</param>
    public DejaVuCompiler(DejaVuParser parser)
    {
        _parser = parser;
        _optimizer = new TemplateOptimizer();
        _symbolResolver = new SymbolResolver(new DiagnosticSink());
    }

    /// <summary>
    ///     编译模板源码——解析 + 优化 + 符号解析，返回可缓存的编译产物
    /// </summary>
    /// <param name="source">模板源码</param>
    /// <param name="templatePath">模板源文件路径（可选，用于缓存失效和错误定位）</param>
    /// <param name="emitSymbolTable">是否输出符号表（用于 IDE 智能提示等）</param>
    /// <param name="emitRenderFunc">是否生成渲染委托（JIT 编译，用于高性能渲染）</param>
    /// <returns>编译后的模板</returns>
    public CompiledTemplate Compile(string source, string templatePath = "", bool emitSymbolTable = false, bool emitRenderFunc = false)
    {
        var parseResult = _parser.Parse(source);
        var optimizedNodes = _optimizer.Optimize(parseResult.Nodes.ToList());

        var symbolTable = _symbolResolver.Resolve(optimizedNodes);

        Func<IDictionary<string, object>, string>? renderFunc = null;
        if (emitRenderFunc)
        {
            var codeGen = new TemplateCodeGenerator();
            renderFunc = codeGen.Compile(optimizedNodes);
        }

        return new CompiledTemplate
        {
            TemplatePath = templatePath,
            Nodes = optimizedNodes,
            CompiledAt = DateTimeOffset.UtcNow,
            SourceLastWriteTime = GetSourceLastWriteTime(templatePath),
            SymbolTable = emitSymbolTable ? symbolTable : null,
            RenderFunc = renderFunc
        };
    }

    /// <summary>
    ///     生成 TypeScript 渲染函数源码
    /// </summary>
    /// <param name="source">模板源码</param>
    /// <param name="templateName">模板函数名</param>
    /// <param name="options">TypeScript 生成选项</param>
    /// <returns>TypeScript 源码</returns>
    public string CompileToTypeScript(string source, string templateName = "render", TypeScriptGeneratorOptions? options = null)
    {
        var parseResult = _parser.Parse(source);
        var optimizedNodes = _optimizer.Optimize(parseResult.Nodes.ToList());
        var generator = options != null
            ? new TypeScriptCodeGenerator(options)
            : new TypeScriptCodeGenerator();
        return generator.Generate(optimizedNodes, templateName);
    }

    /// <summary>
    ///     从模板推导 TypeScript Data 接口
    /// </summary>
    /// <param name="source">模板源码</param>
    /// <param name="interfaceName">接口名称</param>
    /// <returns>TypeScript 接口源码</returns>
    public string InferTypeScriptInterface(string source, string interfaceName = "TemplateData")
    {
        var parseResult = _parser.Parse(source);
        var optimizedNodes = _optimizer.Optimize(parseResult.Nodes.ToList());
        var inferrer = new TypeScriptTypeInferrer();
        return inferrer.InferInterface(optimizedNodes, interfaceName);
    }

    /// <summary>
    ///     执行编译期类型检查
    /// </summary>
    /// <param name="source">模板源码</param>
    /// <param name="knownTypes">已知变量类型（从外部 Data 类型注解提供）</param>
    /// <returns>推导出的变量类型表</returns>
    public Dictionary<string, TemplateType> CheckTypes(string source, Dictionary<string, TemplateType>? knownTypes = null)
    {
        var parseResult = _parser.Parse(source);
        var optimizedNodes = _optimizer.Optimize(parseResult.Nodes.ToList());
        var typeChecker = new TypeChecker(new DiagnosticSink(), new FilterRegistry());
        return typeChecker.Check(optimizedNodes, knownTypes);
    }

    /// <summary>
    ///     生成 Java 渲染类源码
    /// </summary>
    /// <param name="source">模板源码</param>
    /// <param name="className">Java 类名</param>
    /// <param name="options">Java 生成选项</param>
    /// <returns>Java 源码</returns>
    public string CompileToJava(string source, string className = "TemplateRenderer", JavaGeneratorOptions? options = null)
    {
        var parseResult = _parser.Parse(source);
        var optimizedNodes = _optimizer.Optimize(parseResult.Nodes.ToList());
        var generator = options != null
            ? new JavaCodeGenerator(options)
            : new JavaCodeGenerator();
        return generator.Generate(optimizedNodes, className);
    }

    private static DateTimeOffset GetSourceLastWriteTime(string templatePath)
    {
        if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
        {
            return DateTimeOffset.MinValue;
        }

        return File.GetLastWriteTimeUtc(templatePath);
    }
}
