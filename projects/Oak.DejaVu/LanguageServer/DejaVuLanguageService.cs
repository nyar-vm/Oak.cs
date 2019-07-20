using Oak.Diagnostics;
using Oak.DejaVu.CodeGen;
using Oak.DejaVu.Filters;
using Oak.DejaVu.Optimizer;

namespace Oak.DejaVu.LanguageServer;

/// <summary>
///     DejaVu 语言服务——提供文档诊断、自动补全、悬停提示等 LSP 功能。
///     作为 LSP 服务器后端，不依赖具体的 LSP 传输协议。
/// </summary>
public sealed class DejaVuLanguageService
{
    private readonly DejaVuCompiler _compiler;
    private readonly FilterRegistry _filters;
    private readonly TemplateStandardLibrary _standardLibrary;
    private readonly Dictionary<string, DocumentState> _documents = new();

    /// <summary>
    ///     创建 DejaVu 语言服务
    /// </summary>
    public DejaVuLanguageService()
    {
        _compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        _filters = new FilterRegistry();
        _standardLibrary = new TemplateStandardLibrary();
    }

    /// <summary>
    ///     打开文档——解析并缓存
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <param name="source">文档源码</param>
    /// <returns>初始诊断</returns>
    public List<LspDiagnostic> OpenDocument(string uri, string source)
    {
        var state = AnalyzeDocument(uri, source);
        _documents[uri] = state;
        return state.Diagnostics;
    }

    /// <summary>
    ///     更新文档——增量解析
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <param name="source">更新后的源码</param>
    /// <returns>更新后的诊断</returns>
    public List<LspDiagnostic> UpdateDocument(string uri, string source)
    {
        var state = AnalyzeDocument(uri, source);
        _documents[uri] = state;
        return state.Diagnostics;
    }

    /// <summary>
    ///     关闭文档——移除缓存
    /// </summary>
    public void CloseDocument(string uri)
    {
        _documents.Remove(uri);
    }

    /// <summary>
    ///     获取自动补全列表
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <param name="line">行号（0-based）</param>
    /// <param name="character">列号（0-based）</param>
    /// <returns>补全项列表</returns>
    public List<LspCompletionItem> GetCompletions(string uri, int line, int character)
    {
        var items = new List<LspCompletionItem>();

        if (!_documents.TryGetValue(uri, out var state)) return items;

        AddFilterCompletions(items);
        AddStandardHelperCompletions(items);
        AddKeywordCompletions(items);
        AddVariableCompletions(items, state);

        return items;
    }

    /// <summary>
    ///     获取悬停提示
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <param name="line">行号（0-based）</param>
    /// <param name="character">列号（0-based）</param>
    /// <returns>悬停信息，或 null</returns>
    public LspHover? GetHover(string uri, int line, int character)
    {
        if (!_documents.TryGetValue(uri, out var state)) return null;

        var word = ExtractWordAtPosition(state.Source, line, character);
        if (string.IsNullOrEmpty(word)) return null;

        var hoverContent = ResolveHoverContent(word, state);
        if (hoverContent == null) return null;

        return new LspHover
        {
            Range = new LspRange
            {
                Start = new LspPosition { Line = line, Character = character },
                End = new LspPosition { Line = line, Character = character + word.Length }
            },
            Contents = hoverContent
        };
    }

    /// <summary>
    ///     获取文档符号列表（大纲视图）
    /// </summary>
    public List<DocumentSymbol> GetDocumentSymbols(string uri)
    {
        if (!_documents.TryGetValue(uri, out var state)) return [];
        if (state.SymbolTable == null) return [];

        var symbols = new List<DocumentSymbol>();

        foreach (var blockName in state.SymbolTable.Blocks)
        {
            symbols.Add(new DocumentSymbol
            {
                Name = blockName,
                Kind = SymbolKind.Method,
                Detail = "block"
            });
        }

        if (state.SymbolTable.ParentTemplate != null)
        {
            symbols.Add(new DocumentSymbol
            {
                Name = $"extends: {state.SymbolTable.ParentTemplate}",
                Kind = SymbolKind.Module,
                Detail = "继承"
            });
        }

        foreach (var includePath in state.SymbolTable.IncludedTemplates)
        {
            symbols.Add(new DocumentSymbol
            {
                Name = $"include: {includePath}",
                Kind = SymbolKind.File,
                Detail = "引入"
            });
        }

        return symbols;
    }

    private DocumentState AnalyzeDocument(string uri, string source)
    {
        var diagnostics = new DiagnosticSink();

        var parser = new DejaVuParser("doki", diagnostics);
        var parseResult = parser.Parse(source);

        var optimizer = new TemplateOptimizer();
        var optimizedNodes = optimizer.Optimize(parseResult.Nodes.ToList());

        var symbolResolver = new SymbolResolver(diagnostics);
        var symbolTable = symbolResolver.Resolve(optimizedNodes);

        var typeChecker = new TypeChecker(diagnostics, _filters);
        var inferredTypes = typeChecker.Check(optimizedNodes);

        var lspDiagnostics = LspDiagnosticConverter.Convert(diagnostics);

        return new DocumentState
        {
            Source = source,
            Nodes = optimizedNodes,
            SymbolTable = symbolTable,
            InferredTypes = inferredTypes,
            Diagnostics = lspDiagnostics
        };
    }

    private void AddFilterCompletions(List<LspCompletionItem> items)
    {
        var filterNames = new[]
        {
            "uppercase", "lowercase", "trim", "length", "reverse",
            "abs", "round", "floor", "ceil",
            "first", "last", "count", "join",
            "date", "datetime",
            "default", "escape", "safe"
        };

        foreach (var name in filterNames)
        {
            items.Add(new LspCompletionItem
            {
                Label = name,
                Kind = 3,
                Detail = $"过滤器: {name}",
                Documentation = $"DejaVu 内置过滤器 {name}",
                InsertText = name
            });
        }
    }

    private void AddStandardHelperCompletions(List<LspCompletionItem> items)
    {
        foreach (var (name, helper) in _standardLibrary.Helpers)
        {
            var paramList = string.Join(", ", helper.Parameters.Select(p => p.Name));
            items.Add(new LspCompletionItem
            {
                Label = name,
                Kind = 3,
                Detail = $"{helper.Description} ({paramList})",
                Documentation = helper.Description,
                InsertText = $"{name}({paramList})"
            });
        }
    }

    private void AddKeywordCompletions(List<LspCompletionItem> items)
    {
        var keywords = new[]
        {
            ("if", "条件判断", 17),
            ("else if", "条件分支", 17),
            ("else", "默认分支", 17),
            ("loop", "循环遍历", 17),
            ("loop in", "带变量循环", 17),
            ("let", "局部变量绑定", 17),
            ("with", "作用域别名", 17),
            ("block", "块定义", 17),
            ("extends", "模板继承", 17),
            ("include", "引入子模板", 17),
            ("raw", "原始输出", 17),
            ("end", "结束标签", 17),
            ("super()", "父模板默认内容", 3)
        };

        foreach (var (keyword, description, kind) in keywords)
        {
            items.Add(new LspCompletionItem
            {
                Label = keyword,
                Kind = kind,
                Detail = description,
                Documentation = description,
                InsertText = keyword
            });
        }
    }

    private void AddVariableCompletions(List<LspCompletionItem> items, DocumentState state)
    {
        foreach (var (name, type) in state.InferredTypes)
        {
            items.Add(new LspCompletionItem
            {
                Label = name,
                Kind = 6,
                Detail = $"变量: {type}",
                Documentation = $"模板变量 \"{name}\"，类型: {type}",
                InsertText = name
            });
        }
    }

    private string? ResolveHoverContent(string word, DocumentState state)
    {
        if (_standardLibrary.HasHelper(word))
        {
            var helper = _standardLibrary.GetHelper(word)!;
            var paramList = string.Join(", ", helper.Parameters.Select(p => $"{p.Name}: {p.Type}"));
            return $"**{word}**({paramList}): {helper.Description}\n\n输出类型: {helper.OutputType}";
        }

        if (_filters.HasFilter(word))
        {
            return $"**{word}** — DejaVu 内置过滤器";
        }

        if (state.InferredTypes.TryGetValue(word, out var type))
        {
            return $"**{word}**: `{type}` — 模板变量";
        }

        if (state.SymbolTable?.Blocks.Contains(word) == true)
        {
            return $"**block {word}** — 模板块定义";
        }

        return null;
    }

    private static string ExtractWordAtPosition(string source, int line, int character)
    {
        var lines = source.Split('\n');
        if (line < 0 || line >= lines.Length) return string.Empty;

        var lineText = lines[line];
        if (character < 0 || character >= lineText.Length) return string.Empty;

        var start = character;
        while (start > 0 && IsWordChar(lineText[start - 1])) start--;

        var end = character;
        while (end < lineText.Length && IsWordChar(lineText[end])) end++;

        return start < end ? lineText[start..end] : string.Empty;
    }

    private static bool IsWordChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }
}

/// <summary>
///     文档状态缓存
/// </summary>
public sealed class DocumentState
{
    /// <summary>
    ///     文档源码
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    ///     优化后的 AST 节点
    /// </summary>
    public List<DejaVuTemplateNode> Nodes { get; init; } = [];

    /// <summary>
    ///     符号表
    /// </summary>
    public SymbolTable? SymbolTable { get; init; }

    /// <summary>
    ///     推导出的变量类型
    /// </summary>
    public Dictionary<string, TemplateType> InferredTypes { get; init; } = new();

    /// <summary>
    ///     LSP 诊断列表
    /// </summary>
    public List<LspDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>
///     文档符号
/// </summary>
public sealed class DocumentSymbol
{
    /// <summary>
    ///     符号名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     符号类型（1=File, 2=Module, 3=Namespace, 4=Package, 5=Class, 6=Method, 7=Property, 8=Field, 9=Constructor, 10=Enum, 11=Interface, 12=Function, 13=Variable, 14=Constant, 15=String, 16=Number, 17=Boolean, 18=Array, 19=Object, 20=Key, 21=Null, 22=EnumMember, 23=Struct, 24=Event, 25=Operator, 26=TypeParameter）
    /// </summary>
    public int Kind { get; init; }

    /// <summary>
    ///     符号详情
    /// </summary>
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
///     符号类型常量
/// </summary>
public static class SymbolKind
{
    /// <summary>
    ///     文件
    /// </summary>
    public const int File = 1;

    /// <summary>
    ///     模块
    /// </summary>
    public const int Module = 2;

    /// <summary>
    ///     方法
    /// </summary>
    public const int Method = 6;

    /// <summary>
    ///     函数
    /// </summary>
    public const int Function = 12;

    /// <summary>
    ///     变量
    /// </summary>
    public const int Variable = 13;
}
