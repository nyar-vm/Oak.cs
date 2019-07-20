using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Schema;
using Oak.Valkyrie.AST.Statement;
using Oak.Valkyrie.Lexer;

namespace Oak.Valkyrie.Parser;

/// <summary>
///     Valkyrie 语法分析器
/// </summary>
public sealed class ValkyrieParser
{
    private readonly ValkyrieLanguage _language;
    private readonly DiagnosticSink? _diagnostics;

    /// <summary>
    ///     使用默认语言创建分析器
    /// </summary>
    public ValkyrieParser()
    {
        _language = ValkyrieLanguage.Standard;
    }

    /// <summary>
    ///     使用指定语言配置创建分析器
    /// </summary>
    /// <param name="language">语言配置</param>
    public ValkyrieParser(ValkyrieLanguage language)
    {
        _language = language;
    }

    /// <summary>
    ///     使用指定语言配置和诊断接收器创建分析器
    /// </summary>
    /// <param name="language">语言配置</param>
    /// <param name="diagnostics">诊断接收器</param>
    public ValkyrieParser(ValkyrieLanguage language, DiagnosticSink? diagnostics)
    {
        _language = language;
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     解析词法单元列表并生成 AST
    /// </summary>
    /// <param name="tokens">词法单元列表</param>
    /// <returns>AST 根节点</returns>
    public ValkyrieNode Parse(IReadOnlyList<GreenLeafNode> tokens)
    {
        var source = new TokenStream(tokens, _diagnostics);
        var declarations = source.ParseTopLevelNodes(_language);
        var normalizedDeclarations = NormalizeDeclarations(declarations);

        return new ProgramRoot
        {
            Declarations = normalizedDeclarations,
            FilePath = string.Empty
        };
    }

    /// <summary>
    ///     统一按新版 ObjectBody 体系归一化声明节点
    /// </summary>
    private static IReadOnlyList<ValkyrieNode> NormalizeDeclarations(IReadOnlyList<ValkyrieNode> declarations)
    {
        if (declarations.Count == 0)
        {
            return declarations;
        }

        var normalized = new List<ValkyrieNode>(declarations.Count);
        foreach (var declaration in declarations)
        {
            normalized.Add(NormalizeDeclarationNode(declaration));
        }

        return normalized;
    }

    /// <summary>
    ///     递归归一化声明节点，确保对象体使用新版结构
    /// </summary>
    private static ValkyrieNode NormalizeDeclarationNode(ValkyrieNode node)
    {
        switch (node)
        {
            case DeclareClass declareClass:
                return declareClass with
                {
                    Body = NormalizeObjectBody(declareClass.Body)
                };
            case DeclareStructure declareStructure:
                return declareStructure with
                {
                    Body = NormalizeObjectBody(declareStructure.Body)
                };
            case DeclareTrait declareTrait:
                return declareTrait with
                {
                    Body = NormalizeObjectBody(declareTrait.Body)
                };
            case DeclareModel declareModel:
                return declareModel with
                {
                    Body = NormalizeObjectBody(declareModel.Body)
                };
            case DeclareService declareService:
                return declareService with
                {
                    Body = NormalizeObjectBody(declareService.Body)
                };
            case DeclareObjectDomain declareObjectDomain:
                return NormalizeDomain(declareObjectDomain);
            case DeclareUniteVariant declareUniteVariant:
                return declareUniteVariant with
                {
                    Body = NormalizeObjectBody(declareUniteVariant.Body)
                };
            case DeclareNamespace declareNamespace:
            {
                var nestedDeclarations = NormalizeDeclarations(declareNamespace.Declarations);
                return declareNamespace with { Declarations = nestedDeclarations };
            }
            case FunctionBody blockStmt:
            {
                var normalizedStatements = NormalizeDeclarations(blockStmt.Statements);
                return blockStmt with { Statements = normalizedStatements };
            }
            default:
                return node;
        }
    }

    /// <summary>
    ///     归一化子域节点，确保其对象体符合新版结构
    /// </summary>
    private static DeclareObjectDomain NormalizeDomain(DeclareObjectDomain domain)
    {
        return domain with
        {
            Body = NormalizeObjectBody(domain.Body)
        };
    }

    /// <summary>
    ///     归一化对象体并递归处理子域
    /// </summary>
    private static ObjectBody NormalizeObjectBody(ObjectBody? body)
    {
        var normalizedBody = body ?? new ObjectBody();
        if (normalizedBody.Domains.Count == 0)
        {
            return normalizedBody;
        }

        var normalizedDomains = new List<DeclareObjectDomain>(normalizedBody.Domains.Count);
        foreach (var domain in normalizedBody.Domains)
        {
            normalizedDomains.Add(NormalizeDomain(domain));
        }

        return normalizedBody with
        {
            Domains = normalizedDomains
        };
    }
}
