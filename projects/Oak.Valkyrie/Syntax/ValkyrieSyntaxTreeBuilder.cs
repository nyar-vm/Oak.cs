using Oak.Syntax;
using Oak.Valkyrie.Lexer;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     Valkyrie 语法树构建器 —— 创建和管理 Valkyrie 强类型语法树
/// </summary>
public static class ValkyrieSyntaxTreeBuilder
{
    /// <summary>
    ///     Valkyrie 语法节点类型的 NodeKind 起始值，避免与 Oak 内置类型冲突
    /// </summary>
    public const int KindBase = 10000;

    private static bool _registered;

    /// <summary>
    ///     向 NodeFactory 注册所有 Valkyrie 语法节点类型的构造委托
    /// </summary>
    public static void RegisterNodeKinds()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;

        NodeFactory.Register(KindBase + 0, (g, t, o) => new ComponentSyntax(g, t, o));
        NodeFactory.Register(KindBase + 1, (g, t, o) => new SystemSyntax(g, t, o));
        NodeFactory.Register(KindBase + 2, (g, t, o) => new WidgetSyntax(g, t, o));
        NodeFactory.Register(KindBase + 3, (g, t, o) => new FunctionSyntax(g, t, o));
        NodeFactory.Register(KindBase + 4, (g, t, o) => new EnumSyntax(g, t, o));
        NodeFactory.Register(KindBase + 5, (g, t, o) => new ImportSyntax(g, t, o));
        NodeFactory.Register(KindBase + 6, (g, t, o) => new VariableSyntax(g, t, o));
        NodeFactory.Register(KindBase + 7, (g, t, o) => new FieldSyntax(g, t, o));
        NodeFactory.Register(KindBase + 8, (g, t, o) => new ShaderSyntax(g, t, o));
        NodeFactory.Register(KindBase + 9, (g, t, o) => new StorageSyntax(g, t, o));
        NodeFactory.Register(KindBase + 10, (g, t, o) => new ServiceSyntax(g, t, o));
        NodeFactory.Register(KindBase + 11, (g, t, o) => new NamespaceSyntax(g, t, o));
        NodeFactory.Register(KindBase + 12, (g, t, o) => new ParameterSyntax(g, t, o));
        NodeFactory.Register(KindBase + 13, (g, t, o) => new QuerySyntax(g, t, o));

        NodeFactory.Register(KindBase + 99, (g, t, o) => new ValkyrieSyntaxRoot(g, t, o));
    }

    /// <summary>
    ///     从 ISource 和 CstBuilder 构建操作创建 SyntaxTree
    /// </summary>
    public static SyntaxTree Build(ISource source, Action<CstBuilder> buildAction)
    {
        RegisterNodeKinds();

        var builder = new CstBuilder();
        buildAction(builder);
        var green = builder.Build();

        return new SyntaxTree(source, green, enableParentCache: true);
    }

    /// <summary>
    ///     从字符串源码和 CstBuilder 构建操作创建 SyntaxTree
    /// </summary>
    public static SyntaxTree Build(string source, Action<CstBuilder> buildAction)
    {
        return Build(new StringSource(source), buildAction);
    }

    /// <summary>
    ///     从源文本和绿树根节点创建 SyntaxTree（启用父节点缓存）
    /// </summary>
    public static SyntaxTree BuildFromGreen(ISource source, GreenNode greenRoot)
    {
        RegisterNodeKinds();
        return new SyntaxTree(source, greenRoot, enableParentCache: true);
    }

    /// <summary>
    ///     获取 CompilationUnit 的 NodeKind
    /// </summary>
    public static NodeKind CompilationUnitKind => new(KindBase + 99);

    /// <summary>
    ///     获取 ComponentDecl 的 NodeKind
    /// </summary>
    public static NodeKind ComponentKind => new(KindBase + 0);

    /// <summary>
    ///     获取 SystemDecl 的 NodeKind
    /// </summary>
    public static NodeKind SystemKind => new(KindBase + 1);

    /// <summary>
    ///     获取 WidgetDecl 的 NodeKind
    /// </summary>
    public static NodeKind WidgetKind => new(KindBase + 2);

    /// <summary>
    ///     获取 FunctionDecl 的 NodeKind
    /// </summary>
    public static NodeKind FunctionKind => new(KindBase + 3);

    /// <summary>
    ///     获取 EnumDecl 的 NodeKind
    /// </summary>
    public static NodeKind EnumKind => new(KindBase + 4);

    /// <summary>
    ///     获取 ImportDecl 的 NodeKind
    /// </summary>
    public static NodeKind ImportKind => new(KindBase + 5);

    /// <summary>
    ///     获取 VariableDecl 的 NodeKind
    /// </summary>
    public static NodeKind VariableKind => new(KindBase + 6);

    /// <summary>
    ///     获取 FieldDecl 的 NodeKind
    /// </summary>
    public static NodeKind FieldKind => new(KindBase + 7);

    /// <summary>
    ///     获取 ShaderDecl 的 NodeKind
    /// </summary>
    public static NodeKind ShaderKind => new(KindBase + 8);

    /// <summary>
    ///     获取 StorageDecl 的 NodeKind
    /// </summary>
    public static NodeKind StorageKind => new(KindBase + 9);

    /// <summary>
    ///     获取 ServiceDecl 的 NodeKind
    /// </summary>
    public static NodeKind ServiceKind => new(KindBase + 10);

    /// <summary>
    ///     获取 NamespaceDecl 的 NodeKind
    /// </summary>
    public static NodeKind NamespaceKind => new(KindBase + 11);

    /// <summary>
    ///     获取 ParameterDecl 的 NodeKind
    /// </summary>
    public static NodeKind ParameterKind => new(KindBase + 12);

    /// <summary>
    ///     获取 QueryDecl 的 NodeKind
    /// </summary>
    public static NodeKind QueryKind => new(KindBase + 13);

    /// <summary>
    ///     构建 CompilationUnit 的绿树节点
    /// </summary>
    public static GreenNode BuildCompilationUnitGreen(params GreenNode[] declarations)
    {
        var b = new CstBuilder(256, 64);
        b.BeginNode(CompilationUnitKind);
        foreach (var decl in declarations)
        {
            b.AddNode(decl);
        }

        b.EndNode();
        return b.Build();
    }

    /// <summary>
    ///     构建 Component 的绿树节点
    /// </summary>
    public static GreenNode BuildComponentGreen(string name, params GreenNode[] fields)
    {
        var b = new CstBuilder(256, 64);
        b.BeginNode(ComponentKind);
        b.AddToken(ValkyrieTokenKind.Component.ToNodeKind(), "component");
        b.AddToken(ValkyrieTokenKind.Identifier.ToNodeKind(), name);
        b.AddToken(ValkyrieTokenKind.BraceL.ToNodeKind(), "{");

        foreach (var field in fields)
        {
            b.AddNode(field);
        }

        b.AddToken(ValkyrieTokenKind.BraceR.ToNodeKind(), "}");
        b.EndNode();
        return b.Build();
    }

    /// <summary>
    ///     构建 Field 的绿树节点
    /// </summary>
    public static GreenNode BuildFieldGreen(string name, string type, string? defaultValue = null)
    {
        var b = new CstBuilder(256, 64);
        b.BeginNode(FieldKind);
        b.AddToken(ValkyrieTokenKind.Identifier.ToNodeKind(), name);
        b.AddToken(ValkyrieTokenKind.Colon.ToNodeKind(), ":");
        b.AddToken(ValkyrieTokenKind.Identifier.ToNodeKind(), type);

        if (defaultValue != null)
        {
            b.AddToken(ValkyrieTokenKind.Equal.ToNodeKind(), "=");
            b.AddToken(ValkyrieTokenKind.Number.ToNodeKind(), defaultValue);
        }

        b.EndNode();
        return b.Build();
    }

    /// <summary>
    ///     构建 Identifier Token 绿树叶节点
    /// </summary>
    public static GreenNode BuildIdentifierToken(string name)
    {
        return new GreenLeafNode(ValkyrieTokenKind.Identifier.ToNodeKind(), name.Length, name);
    }

    private static readonly Dictionary<string, ValkyrieTokenKind> KeywordMap = new()
    {
        { "let", ValkyrieTokenKind.Let },
        { "micro", ValkyrieTokenKind.Micro },
        { "mezzo", ValkyrieTokenKind.Mezzo },
        { "macro", ValkyrieTokenKind.Macro },
        { "component", ValkyrieTokenKind.Component },
        { "system", ValkyrieTokenKind.System },
        { "widget", ValkyrieTokenKind.Widget },
        { "return", ValkyrieTokenKind.Return },
        { "if", ValkyrieTokenKind.If },
        { "else", ValkyrieTokenKind.Else },
        { "loop", ValkyrieTokenKind.Loop },
        { "while", ValkyrieTokenKind.While },
        { "until", ValkyrieTokenKind.Until },
        { "structure", ValkyrieTokenKind.Structure },
        { "match", ValkyrieTokenKind.Match },
        { "case", ValkyrieTokenKind.Case },
        { "end", ValkyrieTokenKind.End },
        { "namespace", ValkyrieTokenKind.Namespace },
        { "using", ValkyrieTokenKind.Using },
        { "class", ValkyrieTokenKind.Class },
        { "enums", ValkyrieTokenKind.Enums },
        { "flags", ValkyrieTokenKind.Flags },
        { "union", ValkyrieTokenKind.Union },
        { "in", ValkyrieTokenKind.In },
        { "break", ValkyrieTokenKind.Break },
        { "continue", ValkyrieTokenKind.Continue },
        { "resume", ValkyrieTokenKind.Resume },
        { "catch", ValkyrieTokenKind.Catch },
        { "unite", ValkyrieTokenKind.Unite },
        { "type", ValkyrieTokenKind.Type },
        { "where", ValkyrieTokenKind.Where },
        { "model", ValkyrieTokenKind.Model },
        { "service", ValkyrieTokenKind.Service },
        { "message", ValkyrieTokenKind.Message },
        { "trait", ValkyrieTokenKind.Trait },
        { "neural", ValkyrieTokenKind.Neural },
        { "shader", ValkyrieTokenKind.Shader },
        { "vertex", ValkyrieTokenKind.Vertex },
        { "fragment", ValkyrieTokenKind.Fragment },
        { "compute", ValkyrieTokenKind.Compute },
        { "uniform", ValkyrieTokenKind.Uniform },
        { "varying", ValkyrieTokenKind.Varying },
        { "cbuffer", ValkyrieTokenKind.CBuffer },
        { "texture", ValkyrieTokenKind.Texture },
        { "sampler", ValkyrieTokenKind.Sampler },
        { "discard", ValkyrieTokenKind.Discard },
        { "raygen", ValkyrieTokenKind.Raygen },
        { "closesthit", ValkyrieTokenKind.Closesthit },
        { "anyhit", ValkyrieTokenKind.Anyhit },
        { "miss", ValkyrieTokenKind.Miss },
        { "constant", ValkyrieTokenKind.Constant },
        { "binding", ValkyrieTokenKind.Binding },
        { "is", ValkyrieTokenKind.Is },
        { "as", ValkyrieTokenKind.As }
    };

    /// <summary>
    ///     构建关键字 Token 绿树叶节点
    /// </summary>
    public static GreenNode BuildKeywordToken(string keyword)
    {
        if (KeywordMap.TryGetValue(keyword, out var kind))
        {
            return new GreenLeafNode(kind.ToNodeKind(), keyword.Length, keyword);
        }

        return new GreenLeafNode(ValkyrieTokenKind.Identifier.ToNodeKind(), keyword.Length, keyword);
    }

    /// <summary>
    ///     尝试从 SyntaxTree 获取 Valkyrie 语法根
    /// </summary>
    public static ValkyrieSyntaxRoot? GetValkyrieRoot(SyntaxTree tree)
    {
        if (!_registered)
        {
            RegisterNodeKinds();
        }

        var node = NodeFactory.Create(CompilationUnitKind, tree.Root, tree, 0);
        return node as ValkyrieSyntaxRoot;
    }
}
