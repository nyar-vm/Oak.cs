using System.Text;
using Oak.C;
using Oak.Csv;
using Oak.Data;
using Oak.Ktx;
using Oak.Markdown;
using Oak.Markdown.Syntax;
using Oak.Obj;
using Oak.Parsing;
using Oak.Scss;
using Oak.SpineAtlas;
using Oak.Typescript.AST;
using Oak.Typescript.Lexer;
using Oak.Typescript.Parsing;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Oak.Von;

namespace Oak;

/// <summary>
///     Oak 默认配置，预注册所有内置解析器
/// </summary>
public static class OakDefaults
{
    /// <summary>
    ///     创建带有所有默认解析器的注册表
    /// </summary>
    public static OakParserRegistry CreateRegistry()
    {
        var registry = new OakParserRegistry();
        RegisterDefaults(registry);
        return registry;
    }

    /// <summary>
    ///     注册所有默认解析器
    /// </summary>
    public static void RegisterDefaults(OakParserRegistry registry)
    {
        registry.Register<string, MarkdownDocument>("markdown", new MarkdownPipeline());
        registry.Register<string, SerdeValue>("gon", new GonPipeline());
        registry.Register<string, ValkyrieNode>("ggscript", new GgScriptPipeline());
        registry.Register<string, ValkyrieNode>("ggshader", new GgShaderPipeline());
        registry.Register<string, StyleSheet>("scss", new ScssPipeline());
        registry.Register<string, CAstNode>("c", new CPipeline());
        registry.Register<string, TsAstNode>("typescript", new TsPipeline());
        registry.Register<string, ObjParseResult>("obj", new ObjPipeline());
        registry.Register<string, TableFieldType>("tablefieldtype", new TableFieldTypePipeline());
        registry.Register<string, KtxParseResult>("ktx", new KtxPipeline());
        registry.Register<string, SpineAtlasData>("spineatlas", new SpineAtlasPipeline());
    }

    /// <summary>
    ///     Markdown 解析管道（词法 + 语法）
    /// </summary>
    private sealed class MarkdownPipeline : IStringParser<MarkdownDocument>
    {
        public MarkdownDocument Parse(string source)
        {
            var lexer = new MarkdownLexer();
            var tokens = lexer.Tokenize(source);
            var parser = new MarkdownParser();
            return parser.Parse(tokens);
        }
    }

    /// <summary>
    ///     Gon 解析管道
    /// </summary>
    private sealed class GonPipeline : IStringParser<SerdeValue>
    {
        public SerdeValue Parse(string source)
        {
            var parser = new GonParser();
            return parser.Parse(source).Inner;
        }
    }

    /// <summary>
    ///     GGScript 解析管道（词法 + 语法）
    /// </summary>
    private sealed class GgScriptPipeline : IStringParser<ValkyrieNode>
    {
        public ValkyrieNode Parse(string source)
        {
            var lexer = new ValkyrieLexer();
            var tokens = lexer.Tokenize(source);
            var parser = new ValkyrieParser();
            return parser.Parse(tokens);
        }
    }

    /// <summary>
    ///     GGShader 解析管道（词法 + 语法）
    /// </summary>
    private sealed class GgShaderPipeline : IStringParser<ValkyrieNode>
    {
        public ValkyrieNode Parse(string source)
        {
            var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
            var tokens = lexer.Tokenize(source);
            var parser = new ValkyrieParser(ValkyrieLanguage.Shader);
            return parser.Parse(tokens);
        }
    }

    /// <summary>
    ///     SCSS 解析管道
    /// </summary>
    private sealed class ScssPipeline : IStringParser<StyleSheet>
    {
        public StyleSheet Parse(string source)
        {
            return StyleSheet.Parse(source);
        }
    }

    /// <summary>
    ///     TypeScript 解析管道（词法 + 语法）
    /// </summary>
    private sealed class TsPipeline : IStringParser<TsAstNode>
    {
        public TsAstNode Parse(string source)
        {
            var lexer = new TsLexer();
            var tokens = lexer.Tokenize(source);
            var parser = new TsParser();
            return parser.Parse(tokens);
        }
    }

    /// <summary>
    ///     OBJ 3D 模型解析管道
    /// </summary>
    private sealed class ObjPipeline : IStringParser<ObjParseResult>
    {
        public ObjParseResult Parse(string source)
        {
            var parser = new ObjParser();
            return parser.Parse(source.AsSpan());
        }
    }

    /// <summary>
    ///     配置表字段类型解析管道
    /// </summary>
    private sealed class TableFieldTypePipeline : IStringParser<TableFieldType>
    {
        public TableFieldType Parse(string source)
        {
            var parser = new TableFieldTypeParser();
            return parser.Parse(source);
        }
    }

    /// <summary>
    ///     KTX 纹理解析管道
    /// </summary>
    private sealed class KtxPipeline : IStringParser<KtxParseResult>
    {
        public KtxParseResult Parse(string source)
        {
            var parser = new KtxParser();
            return parser.Parse(Encoding.UTF8.GetBytes(source));
        }
    }

    /// <summary>
    ///     Spine Atlas 解析管道
    /// </summary>
    private sealed class SpineAtlasPipeline : IStringParser<SpineAtlasData>
    {
        public SpineAtlasData Parse(string source)
        {
            return SpineAtlasParser.Parse(source);
        }
    }
}