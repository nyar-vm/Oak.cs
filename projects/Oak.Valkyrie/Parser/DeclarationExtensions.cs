using System.Linq;
using System.Text;
using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.ECS;
using Oak.Valkyrie.AST.Neural;
using Oak.Valkyrie.AST.Schema;
using Oak.Valkyrie.AST.Shader;
using Oak.Valkyrie.AST.Statement;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;
using Oak.Valkyrie.Lexer;

namespace Oak.Valkyrie.Parser;

/// <summary>
/// 声明解析扩展入口。
/// </summary>
internal static class DeclarationExtensions
{
    extension(TokenStream tokens)
    {
        internal IReadOnlyList<ValkyrieNode> ParseTopLevelNodes(ValkyrieLanguage language)
        {
            var declarations = new List<ValkyrieNode>();

            while (!tokens.IsAtEnd())
            {
                try
                {
                    var leadingDocs = tokens.ParseDocComments();
                    var leadingAttrs = tokens.CollectLeadingAttributes();
                    var leadingMods = tokens.CollectLeadingModifiers();

                    if (tokens.IsDeclarationStart(language))
                    {
                        declarations.Add(
                            tokens.ParseDeclarationWithModifiers(language, leadingAttrs, leadingMods, leadingDocs));
                    }
                    else
                    {
                        declarations.Add(tokens.ParseStatementNode(language));
                    }
                }
                catch (InvalidOperationException)
                {
                    tokens.Synchronize();
                }
            }

            return declarations;
        }

        internal bool IsDeclarationStartNode(ValkyrieLanguage language)
        {
            return tokens.IsDeclarationStart(language);
        }

        internal ValkyrieNode ParseDeclarationNode(ValkyrieLanguage language)
        {
            return tokens.ParseDeclaration(language);
        }

        internal DeclareLet ParseVariableDeclNode(ValkyrieLanguage language)
        {
            return tokens.ParseVariableDecl(language, []);
        }

        internal FunctionBody ParseBlockNode(ValkyrieLanguage language)
        {
            return tokens.ParseBlock(language);
        }

        internal IReadOnlyList<AttributeItem> CollectLeadingAttributes()
        {
            return tokens.ParseAttributes();
        }

        internal IReadOnlyList<string> CollectLeadingModifiers()
        {
            var count = 0;

            while (!tokens.IsAtEnd())
            {
                var kind = tokens.PeekKind(count);
                if (kind != ValkyrieTokenKind.Identifier)
                {
                    break;
                }

                count++;
            }

            if (count == 0 || !tokens.Peek(count).Kind.IsKeyword())
            {
                return [];
            }

            var mods = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                mods.Add(tokens.AdvanceText());
            }

            return mods;
        }

        internal ValkyrieNode ParseDeclarationWithModifiers(ValkyrieLanguage language,
            IReadOnlyList<AttributeItem> leadingAttrs,
            IReadOnlyList<string> leadingMods,
            IReadOnlyList<DocumentComment>? leadingDocs = null)
        {
            var decl = tokens.ParseDeclaration(language);
            return tokens.AttachLeadingMetadata(decl, leadingAttrs, leadingMods, leadingDocs);
        }

        private ValkyrieNode AttachLeadingMetadata(ValkyrieNode node,
            IReadOnlyList<AttributeItem> attrs,
            IReadOnlyList<string> mods,
            IReadOnlyList<DocumentComment>? docs = null)
        {
            if (attrs.Count == 0 && mods.Count == 0 && (docs is null || docs.Count == 0))
            {
                return node;
            }

            switch (node)
            {
                case DeclareComponent comp:
                    comp = comp with { Annotations = MergeAnnotations(comp.Annotations, attrs, mods, docs) };
                    return comp;
                case DeclareSystem sys:
                    sys = sys with { Annotations = MergeAnnotations(sys.Annotations, attrs, mods, docs) };
                    return sys;
                case DeclareWidget w:
                    w = w with { Annotations = MergeAnnotations(w.Annotations, attrs, mods, docs) };
                    return w;
                case DeclareMicro fn:
                    fn = fn with { Annotations = MergeAnnotations(fn.Annotations, attrs, mods, docs) };
                    return fn;
                case DeclareEnums e:
                    e = e with { Annotations = MergeAnnotations(e.Annotations, attrs) };
                    return e;
                case ShaderDecl s:
                    s = s with { Annotations = MergeAnnotations(s.Annotations, attrs) };
                    return s;
                case DeclareStructure st:
                    st = st with { Annotations = MergeAnnotations(st.Annotations, attrs) };
                    return st;
                case UniformDecl u:
                    if (attrs.Count > 0) u = u with { Attributes = MergeLists(u.Attributes, attrs) };
                    return u;
                case VaryingDecl v:
                    if (attrs.Count > 0) v = v with { Attributes = MergeLists(v.Attributes, attrs) };
                    return v;
                case ConstantBufferDecl c:
                    if (attrs.Count > 0) c = c with { Attributes = MergeLists(c.Attributes, attrs) };
                    return c;
                case TextureDecl t:
                    if (attrs.Count > 0) t = t with { Attributes = MergeLists(t.Attributes, attrs) };
                    return t;
                case SamplerDecl sm:
                    if (attrs.Count > 0) sm = sm with { Attributes = MergeLists(sm.Attributes, attrs) };
                    return sm;
            }

            return node;
        }
    }

    private static IReadOnlyList<AttributeItem> MergeLists(IReadOnlyList<AttributeItem> existing,
        IReadOnlyList<AttributeItem> leading)
    {
        if (existing.Count == 0) return leading;
        var merged = new List<AttributeItem>(leading.Count + existing.Count);
        merged.AddRange(leading);
        merged.AddRange(existing);
        return merged;
    }

    private static IReadOnlyList<string> MergeLists(IReadOnlyList<string> existing, IReadOnlyList<string> leading)
    {
        if (existing.Count == 0) return leading;
        var merged = new List<string>(leading.Count + existing.Count);
        merged.AddRange(leading);
        merged.AddRange(existing);
        return merged;
    }

    private static IReadOnlyList<DocumentComment> MergeLists(IReadOnlyList<DocumentComment> existing,
        IReadOnlyList<DocumentComment> leading)
    {
        if (existing.Count == 0) return leading;
        var merged = new List<DocumentComment>(leading.Count + existing.Count);
        merged.AddRange(leading);
        merged.AddRange(existing);
        return merged;
    }

    private static Annotations BuildAnnotations(
        IReadOnlyList<AttributeItem>? attrs = null,
        IReadOnlyList<string>? mods = null,
        IReadOnlyList<DocumentComment>? docs = null)
    {
        IReadOnlyList<AttributeItem> attributeItems = attrs ?? [];
        IReadOnlyList<AttributeList> attributeLists = attributeItems.Count == 0
            ? []
            : [new AttributeList { Items = attributeItems }];

        IReadOnlyList<IdentifierNode> modifierNodes = mods is null || mods.Count == 0
            ? []
            : mods.Select(m => new IdentifierNode(m)).ToList();

        return new Annotations
        {
            Documents = docs ?? [],
            AttributeLists = attributeLists,
            Modifiers = modifierNodes
        };
    }

    private static Annotations MergeAnnotations(
        Annotations existing,
        IReadOnlyList<AttributeItem> attrs,
        IReadOnlyList<string>? mods = null,
        IReadOnlyList<DocumentComment>? docs = null)
    {
        var mergedAttributeLists = attrs.Count == 0
            ? existing.AttributeLists
            : [new AttributeList { Items = attrs }, ..existing.AttributeLists];

        IReadOnlyList<IdentifierNode> leadingModifiers = mods is null || mods.Count == 0
            ? []
            : mods.Select(m => new IdentifierNode(m)).ToList();
        var mergedModifiers = leadingModifiers.Count == 0
            ? existing.Modifiers
            : [..leadingModifiers, ..existing.Modifiers];

        var mergedDocuments = docs is null || docs.Count == 0
            ? existing.Documents
            : [..docs, ..existing.Documents];

        return existing with
        {
            AttributeLists = mergedAttributeLists,
            Modifiers = mergedModifiers,
            Documents = mergedDocuments
        };
    }

    extension(TokenStream tokens)
    {
        internal bool IsDeclarationStart(ValkyrieLanguage language)
        {
            if (tokens.IsAtEnd())
            {
                return false;
            }

            var kind = tokens.PeekKind();
            if (kind is ValkyrieTokenKind.BracketL or
                ValkyrieTokenKind.Component or
                ValkyrieTokenKind.System or
                ValkyrieTokenKind.Widget or
                ValkyrieTokenKind.Micro or
                ValkyrieTokenKind.Let or
                ValkyrieTokenKind.Structure or
                ValkyrieTokenKind.Class or
                ValkyrieTokenKind.Enums or
                ValkyrieTokenKind.Flags or
                ValkyrieTokenKind.Union or
                ValkyrieTokenKind.Unite or
                ValkyrieTokenKind.Type or
                ValkyrieTokenKind.Namespace or
                ValkyrieTokenKind.Using or
                ValkyrieTokenKind.Shader or
                ValkyrieTokenKind.Service or
                ValkyrieTokenKind.Model or
                ValkyrieTokenKind.Trait or
                ValkyrieTokenKind.Neural)
            {
                return true;
            }

            if (kind == ValkyrieTokenKind.Identifier)
            {
                if (string.Equals(tokens.PeekText(), "plugin", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var offset = 0;
                while (tokens.PeekKind(offset) == ValkyrieTokenKind.Identifier)
                {
                    offset++;
                }

                var afterModifiers = tokens.Peek(offset);
                return afterModifiers.Kind.IsKeyword() ||
                       afterModifiers.Kind == ValkyrieTokenKind.ParenthesisL.ToNodeKind() && offset >= 2;
            }

            return false;
        }

        internal ValkyrieNode ParseDeclaration(ValkyrieLanguage language)
        {
            if (tokens.IsKeyword())
            {
                return tokens.DispatchKeyword(language, []);
            }
            
            var modifiers = new List<string> { tokens.AdvanceText() };
            modifiers.AddRange(tokens.ParseModifiers());

            if (tokens.IsKeyword())
            {
                return tokens.DispatchKeyword(language, modifiers);
            }

            if (tokens.Check(ValkyrieTokenKind.ParenthesisL, 1))
            {
                return tokens.ParseFunctionDecl(language, modifiers);
            }

            return tokens.SkipUnknownDecl();
        }

        private ValkyrieNode DispatchKeyword(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var vKind = (ValkyrieTokenKind)tokens.Current.Kind.Value;
            tokens.Advance();

            return vKind switch
            {
                ValkyrieTokenKind.Micro => tokens.ParseFunctionDecl(language, modifiers),
                ValkyrieTokenKind.Component => tokens.ParseComponentDecl(language, modifiers),
                ValkyrieTokenKind.System => tokens.ParseSystemDecl(language, modifiers),
                ValkyrieTokenKind.Widget => tokens.ParseWidgetDecl(language, modifiers),
                ValkyrieTokenKind.Model => tokens.ParseModelDeclaration(),
                ValkyrieTokenKind.Service => tokens.ParseServiceDeclaration(),
                ValkyrieTokenKind.Enums => tokens.ParseEnumDecl(),
                ValkyrieTokenKind.Flags => tokens.ParseFlagsDecl(),
                ValkyrieTokenKind.Union => tokens.ParseUnionDecl(),
                ValkyrieTokenKind.Unite => tokens.ParseUniteDecl(),
                ValkyrieTokenKind.Namespace => tokens.ParseNamespaceDecl(),
                ValkyrieTokenKind.Class => tokens.ParseClass(language),
                ValkyrieTokenKind.Structure => tokens.ParseStructDecl(),
                ValkyrieTokenKind.Trait => tokens.ParseTrait(language),
                ValkyrieTokenKind.Neural => tokens.ParseNeuralDecl(),
                ValkyrieTokenKind.Using => tokens.ParseUsingDecl(),
                ValkyrieTokenKind.Type => tokens.ParseTypeAliasDecl(),
                ValkyrieTokenKind.Let => tokens.ParseVariableDecl(language, modifiers),
                ValkyrieTokenKind.Shader => tokens.ParseShaderDecl(),
                ValkyrieTokenKind.Uniform => tokens.ParseUniformDecl(),
                ValkyrieTokenKind.Varying => tokens.ParseVaryingDecl(),
                ValkyrieTokenKind.CBuffer => tokens.ParseConstantBufferDecl(),
                ValkyrieTokenKind.Texture => tokens.ParseTextureDecl(),
                ValkyrieTokenKind.Sampler => tokens.ParseSamplerDecl(),
                _ => tokens.SkipUnknownDecl()
            };
        }

        internal DeclareMicro ParseFunctionDecl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var name = tokens.AdvanceText();

            var typeParameters = tokens.ParseTypeParameters();
            var attributes = tokens.ParseAttributes();
            var docComments = tokens.ParseDocComments();

            tokens.Expect(ValkyrieTokenKind.ParenthesisL);
            var parameters = tokens.ParseParamList();
            tokens.Expect(ValkyrieTokenKind.ParenthesisR);

            TypeNode? returnType = null;
            if (tokens.Check(ValkyrieTokenKind.Colon))
            {
                tokens.Advance();
                returnType = tokens.ParseType();
            }

            var genericConstraints = tokens.ParseGenericConstraints();

            FunctionBody? body = null;
            if (tokens.Check(ValkyrieTokenKind.BraceL))
            {
                body = tokens.ParseBlock(language);
            }
            else
            {
                tokens.Match(ValkyrieTokenKind.Semicolon);
            }

            return new DeclareMicro
            {
                Name = new IdentifierNode(name),
                TypeParameters = typeParameters,
                Parameters = parameters,
                ReturnType = returnType,
                Body = body,
                Annotations = BuildAnnotations(attributes, modifiers, docComments),
                GenericConstraints = genericConstraints,
            };
        }

        internal FunctionBody ParseBlock(ValkyrieLanguage language)
        {
            tokens.Expect(ValkyrieTokenKind.BraceL);
            var stmts = new List<ValkyrieNode>();

            while (!tokens.IsAtEnd() && !tokens.Check(ValkyrieTokenKind.BraceR))
            {
                stmts.Add(tokens.ParseStatementNode(language));
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);
            return new FunctionBody(stmts);
        }

        private DeclareComponent ParseComponentDecl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var name = tokens.AdvanceText();

            if (tokens.Check(ValkyrieTokenKind.Less))
            {
                tokens.Advance();
                while (!tokens.IsAtEnd()
                       && !tokens.Check(ValkyrieTokenKind.Greater))
                {
                    tokens.Advance();
                }

                tokens.Advance();
            }

            var attrs = tokens.ParseAttributes();
            var docs = tokens.ParseDocComments();
            var body = tokens.ParseObjectBody(language);

            return new DeclareComponent
            {
                Name = new IdentifierNode(name),
                Annotations = BuildAnnotations(attrs, modifiers, docs),
                Body = body,
            };
        }

        private DeclareSystem ParseSystemDecl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            var docs = tokens.ParseDocComments();
            var queries = new List<QueryExpr>();
            var body = tokens.ParseObjectBody(language, queries, allowFields: false, allowDomains: false);

            return new DeclareSystem
            {
                Name = new IdentifierNode(name),
                Annotations = BuildAnnotations(attrs, modifiers, docs),
                Queries = queries,
                Body = body,
            };
        }

        private DeclareWidget ParseWidgetDecl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            var docs = tokens.ParseDocComments();

            tokens.Expect(ValkyrieTokenKind.BraceL);
            var props = new List<DeclareObjectField>();
            DeclareMicro? renderMethod = null;

            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                if (tokens.IsFunctionDeclFollows())
                {
                    var blockMods = tokens.CollectBlockFunctionModifiers();
                    renderMethod = tokens.ParseFunctionDecl(language, blockMods);
                }
                else if (tokens.PeekKind() == ValkyrieTokenKind.Identifier)
                {
                    props.Add(tokens.ParseField());
                }
                else
                {
                    tokens.SkipUnrecognizedToken();
                }
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);

            return new DeclareWidget
            {
                Name = new IdentifierNode(name),
                Properties = props,
                RenderMethod = renderMethod,
                Annotations = BuildAnnotations(attrs, modifiers, docs)
            };
        }


        private bool IsIdentifierFollowedByEquals()
        {
            return tokens.PeekKind(1) == ValkyrieTokenKind.Equal;
        }

        private DeclareEnums ParseEnumDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();

            tokens.Expect(ValkyrieTokenKind.BraceL);
            var members = new List<DeclareSemanticMember>();

            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                var memberName = tokens.AdvanceText();
                ValkyrieNode? value = null;
                if (tokens.Check(ValkyrieTokenKind.Equal))
                {
                    tokens.Advance();
                    value = tokens.ParseExpressionNode();
                }

                members.Add(new DeclareSemanticMember { Name = new IdentifierNode(memberName), Value = value as TermNode });
                tokens.Match(ValkyrieTokenKind.Comma);
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);
            return new DeclareEnums
            {
                Name = new IdentifierNode(name),
                Members = members,
                Annotations = BuildAnnotations(attrs)
            };
        }

        private DeclareFlags ParseFlagsDecl()
        {
            var enumDecl = tokens.ParseEnumDecl();
            return new DeclareFlags
            {
                Name = new IdentifierNode(enumDecl.Name.Name),
                Members = enumDecl.Members,
                Annotations = enumDecl.Annotations
            };
        }

        private DeclareUnite ParseUnionDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();

            tokens.Expect(ValkyrieTokenKind.BraceL);
            var variants = new List<DeclareUniteVariant>();

            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                var varName = tokens.AdvanceText();
                var fields = new List<DeclareObjectField>();

                if (tokens.Check(ValkyrieTokenKind.ParenthesisL) || tokens.Check(ValkyrieTokenKind.BraceL))
                {
                    var closeKind = tokens.Check(ValkyrieTokenKind.ParenthesisL)
                        ? ValkyrieTokenKind.ParenthesisR
                        : ValkyrieTokenKind.BraceR;

                    tokens.Advance();
                    while (!tokens.Check(closeKind) && !tokens.IsAtEnd())
                    {
                        fields.Add(tokens.ParseField());
                        tokens.Match(ValkyrieTokenKind.Comma);
                    }

                    tokens.Expect(closeKind);
                }

                variants.Add(new DeclareUniteVariant
                {
                    Name = new IdentifierNode(varName),
                    Body = new ObjectBody
                    {
                        Fields = fields
                    }
                });
                tokens.Match(ValkyrieTokenKind.Comma);
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);
            return new DeclareUnite
            {
                Name = new IdentifierNode(name),
                Variants = variants,
                Annotations = BuildAnnotations(attrs)
            };
        }

        private DeclareUnite ParseUniteDecl()
        {
            return tokens.ParseUnionDecl();
        }

        private DeclareUsing ParseUsingDecl()
        {
            var path = tokens.AdvanceText();

            while (tokens.Check(ValkyrieTokenKind.Dot))
            {
                tokens.Advance();
                path += "." + tokens.AdvanceText();
            }

            string? alias = null;
            if (tokens.Check(ValkyrieTokenKind.As))
            {
                tokens.Advance();
                alias = tokens.AdvanceText();
            }

            tokens.Match(ValkyrieTokenKind.Semicolon);
            return new DeclareUsing { ModulePath = path, Alias = alias };
        }

        private DeclareNamespace ParseNamespaceDecl()
        {
            var isPrimary = false;
            if (tokens.Check(ValkyrieTokenKind.Bang))
            {
                tokens.Advance();
                isPrimary = true;
            }

            var name = tokens.ParseDottedName();
            var attrs = tokens.ParseAttributes();

            if (isPrimary || tokens.Check(ValkyrieTokenKind.Semicolon))
            {
                tokens.Match(ValkyrieTokenKind.Semicolon);
                return new DeclareNamespace
                {
                    Name = new IdentifierNode(name),
                    IsPrimary = isPrimary,
                    Declarations = [],
                    Attributes = attrs
                };
            }

            tokens.Expect(ValkyrieTokenKind.BraceL);
            var decls = new List<ValkyrieNode>();

            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                if (tokens.IsDeclarationStart(ValkyrieLanguage.Standard))
                {
                    decls.Add(tokens.ParseDeclaration(ValkyrieLanguage.Standard));
                }
                else
                {
                    decls.Add(tokens.ParseStatementNode(ValkyrieLanguage.Standard));
                }
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);

            return new DeclareNamespace
            {
                Name = new IdentifierNode(name),
                Declarations = decls,
                Attributes = attrs
            };
        }

        private DeclareModel ParseModelDeclaration()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            var docs = tokens.ParseDocComments();
            var body = tokens.ParseObjectBody(ValkyrieLanguage.Standard, allowMethods: false, allowDomains: false);

            return new DeclareModel
            {
                Name = new IdentifierNode(name),
                Body = body,
                Annotations = BuildAnnotations(attrs, docs: docs)
            };
        }

        private DeclareService ParseServiceDeclaration()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();

            tokens.Expect(ValkyrieTokenKind.BraceL);

            tokens.Expect(ValkyrieTokenKind.BraceR);

            return new DeclareService
            {
                Name = new IdentifierNode(name),
                Annotations = BuildAnnotations(attrs)
            };
        }

        private NeuralDecl ParseNeuralDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();

            tokens.Expect(ValkyrieTokenKind.BraceL);
            var layers = new List<NeuralLayerDecl>();

            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                if (tokens.Check(ValkyrieTokenKind.Identifier))
                {
                    layers.Add(tokens.ParseGenericNeuralLayer());
                }
                else
                {
                    tokens.SkipUnrecognizedToken();
                }
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);

            return new NeuralDecl { Name = name, Attributes = attrs, Layers = layers };
        }

        private NeuralLayerDecl ParseGenericNeuralLayer()
        {
            var layerKind = tokens.AdvanceText();
            var name = tokens.AdvanceText();
            tokens.Expect(ValkyrieTokenKind.BraceL);
            var parameters = new List<NeuralLayerParamDecl>();
            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                var paramName = tokens.AdvanceText();
                tokens.Expect(ValkyrieTokenKind.Equal);
                var value = tokens.ParseExpressionNode();
                if (value is TermAtomicLiteral literal)
                {
                    parameters.Add(new NeuralLayerParamDecl { Name = paramName, Value = literal });
                }
                else
                {
                    tokens.Diagnostics?.AddError("", new TextSpan(tokens.Position, 1), "PARSE", "Neural 层参数必须是字面量");
                }

                tokens.Match(ValkyrieTokenKind.Semicolon);
                tokens.Match(ValkyrieTokenKind.Comma);
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);

            return new SimpleLayerDecl { LayerKind = layerKind, Name = name, Parameters = parameters };
        }

        private DeclareObjectDomain ParseDomainDecl(ValkyrieLanguage language)
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            var body = tokens.ParseObjectBody(language);

            return new DeclareObjectDomain
            {
                Name = name,
                Attributes = attrs,
                Body = body
            };
        }

        private ObjectBody ParseObjectBody(
            ValkyrieLanguage language,
            List<QueryExpr>? queries = null,
            bool allowFields = true,
            bool allowMethods = true,
            bool allowDomains = true)
        {
            tokens.Expect(ValkyrieTokenKind.BraceL);

            var fields = new List<DeclareObjectField>();
            var methods = new List<DeclareObjectMethod>();
            var domains = new List<DeclareObjectDomain>();

            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                if (queries is not null
                    && tokens.PeekKind() == ValkyrieTokenKind.Identifier
                    && tokens.PeekText().StartsWith("query", StringComparison.OrdinalIgnoreCase))
                {
                    if (tokens.IsIdentifierFollowedByEquals())
                    {
                        queries.Add(tokens.ParseNamedQueryExpr());
                    }
                    else
                    {
                        queries.Add(tokens.ParseQueryExpr());
                    }

                    continue;
                }

                if (allowMethods && tokens.IsFunctionDeclFollows())
                {
                    var blockMods = tokens.CollectBlockFunctionModifiers();
                    var method = tokens.ParseFunctionDecl(language, blockMods);
                    methods.Add(new DeclareObjectMethod
                    {
                        Name = method.Name,
                        Annotations = method.Annotations
                    });
                    continue;
                }

                if (allowDomains
                    && tokens.PeekKind() == ValkyrieTokenKind.Identifier
                    && tokens.PeekKind(1) == ValkyrieTokenKind.BraceL)
                {
                    domains.Add(tokens.ParseDomainDecl(language));
                    continue;
                }

                if (allowFields && tokens.IsFieldStart())
                {
                    fields.Add(tokens.ParseField());
                    continue;
                }

                tokens.SkipUnrecognizedToken();
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);

            return new ObjectBody
            {
                Fields = fields,
                Methods = methods,
                Domains = domains
            };
        }

        private DeclareTraitAlias ParseTypeAliasDecl()
        {
            var name = tokens.AdvanceText();
            tokens.Expect(ValkyrieTokenKind.Equal);
            var target = tokens.ParseType();
            tokens.Match(ValkyrieTokenKind.Semicolon);

            return new DeclareTraitAlias { Name = new IdentifierNode(name), TargetType = target };
        }

        private ShaderDecl ParseShaderDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            tokens.Expect(ValkyrieTokenKind.BraceL);
            var stages = new List<ShaderStageDecl>();
            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                if (!tokens.IsKeyword())
                {
                    tokens.SkipUnrecognizedToken();
                    continue;
                }

                var keyword = tokens.PeekText().ToLowerInvariant();
                if (keyword == "vertex")
                {
                    stages.Add(tokens.ParseShaderStage<VertexShaderDecl>("vertex"));
                }
                else if (keyword == "fragment")
                {
                    stages.Add(tokens.ParseShaderStage<FragmentShaderDecl>("fragment"));
                }
                else if (keyword == "compute")
                {
                    stages.Add(tokens.ParseShaderStage<ComputeShaderDecl>("compute"));
                }
                else
                {
                    tokens.SkipUnrecognizedToken();
                }
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);
            return new ShaderDecl
            {
                Name = new IdentifierNode(name),
                Annotations = BuildAnnotations(attrs),
                Stages = stages
            };
        }

        private T ParseShaderStage<T>(string keyword) where T : ShaderStageDecl, new()
        {
            tokens.Advance();
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            tokens.Expect(ValkyrieTokenKind.ParenthesisL);
            tokens.Expect(ValkyrieTokenKind.ParenthesisR);
            var body = new List<ValkyrieNode>();
            if (tokens.Check(ValkyrieTokenKind.BraceL))
            {
                tokens.Advance();
                while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
                {
                    body.Add(tokens.ParseStatementNode(ValkyrieLanguage.Standard));
                }

                tokens.Expect(ValkyrieTokenKind.BraceR);
            }

            return new T { Name = name, Attributes = attrs, Body = body };
        }

        private DeclareStructure ParseStructDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            var body = tokens.ParseObjectBody(ValkyrieLanguage.Standard, allowMethods: false, allowDomains: false);
            return new DeclareStructure
            {
                Name = new IdentifierNode(name),
                Body = body,
                Annotations = BuildAnnotations(attrs)
            };
        }

        private ValkyrieNode ParseClass(ValkyrieLanguage language)
        {
            if (tokens.Check(ValkyrieTokenKind.BraceL))
            {
                var anonymousBody = tokens.ParseObjectBody(language);
                var anonymousGenericConstraints = tokens.ParseGenericConstraints();

                return new DeclareClass
                {
                    Name = new IdentifierNode("__anonymous__"),
                    Body = anonymousBody,
                    TypeParameters = tokens.ParseTypeParameters(),
                    GenericConstraints = anonymousGenericConstraints
                };
            }

            var name = tokens.AdvanceText();
            IReadOnlyList<InheritanceItem>? inheritItems = null;

            if (tokens.Check(ValkyrieTokenKind.ParenthesisL))
            {
                tokens.Advance();
                var items = new List<InheritanceItem>();

                while (!tokens.Check(ValkyrieTokenKind.ParenthesisR) && !tokens.IsAtEnd())
                {
                    items.Add(tokens.ParseInheritanceItem());

                    if (tokens.Check(ValkyrieTokenKind.Comma))
                    {
                        tokens.Advance();
                        continue;
                    }

                    break;
                }

                tokens.Expect(ValkyrieTokenKind.ParenthesisR);
                inheritItems = items;
            }

            var typeParameters = tokens.ParseTypeParameters();
            var attrs = tokens.ParseAttributes();
            var body = tokens.ParseObjectBody(language);
            var genericConstraints = tokens.ParseGenericConstraints();

            return new DeclareClass
            {
                Name = new IdentifierNode(name),
                Inheritance = inheritItems is null ? null : new InheritanceList(inheritItems),
                TypeParameters = typeParameters,
                Body = body,
                Annotations = BuildAnnotations(attrs),
                GenericConstraints = genericConstraints
            };
        }

        private ValkyrieNode ParseTrait(ValkyrieLanguage language)
        {
            if (tokens.Check(ValkyrieTokenKind.BraceL))
            {
                var anonymousBody = tokens.ParseObjectBody(language, allowFields: false, allowDomains: false);

                var anonymousTypeParameters = tokens.ParseTypeParameters();
                var anonymousGenericConstraints = tokens.ParseGenericConstraints();

                return new DeclareTrait
                {
                    Name = new IdentifierNode("__anonymous__"), TypeParameters = anonymousTypeParameters,
                    Body = anonymousBody, GenericConstraints = anonymousGenericConstraints
                };
            }

            var name = tokens.AdvanceText();

            var typeParameters = tokens.ParseTypeParameters();
            ObjectBody? body = null;

            if (tokens.Check(ValkyrieTokenKind.Equal))
            {
                tokens.Advance();
                _ = tokens.ParseType();
            }
            else
            {
                body = tokens.ParseObjectBody(language, allowFields: false, allowDomains: false);
            }

            var genericConstraints = tokens.ParseGenericConstraints();
            return new DeclareTrait
            {
                Name = new IdentifierNode(name), TypeParameters = typeParameters, Body = body,
                GenericConstraints = genericConstraints
            };
        }

        private InheritanceItem ParseInheritanceItem()
        {
            if (tokens.Check(ValkyrieTokenKind.Identifier))
            {
                var nameText = tokens.PeekText();

                if (tokens.PeekText(1) == ":")
                {
                    tokens.Advance();
                    tokens.Advance();

                    var baseType = tokens.ParseType();
                    return new InheritanceItem
                    {
                        Name = new IdentifierNode { Name = nameText },
                        BaseType = baseType
                    };
                }
            }

            var type = tokens.ParseType();
            return new InheritanceItem
            {
                BaseType = type
            };
        }

        private UniformDecl ParseUniformDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            tokens.Expect(ValkyrieTokenKind.Colon);
            var type = tokens.ParseType();
            int? group = null;
            int? binding = null;

            if (tokens.Check(ValkyrieTokenKind.Identifier) && tokens.PeekText() == "group")
            {
                tokens.Advance();
                group = tokens.SafeParseInt(tokens.AdvanceText());
            }

            if (tokens.Check(ValkyrieTokenKind.Binding))
            {
                tokens.Advance();
                binding = tokens.SafeParseInt(tokens.AdvanceText());
            }
            else if (tokens.Check(ValkyrieTokenKind.Identifier) && tokens.PeekText() == "binding")
            {
                tokens.Advance();
                binding = tokens.SafeParseInt(tokens.AdvanceText());
            }

            tokens.Match(ValkyrieTokenKind.Semicolon);
            tokens.Match(ValkyrieTokenKind.Comma);
            return new UniformDecl
                { Name = name, UniformType = type, Group = group, Binding = binding, Attributes = attrs };
        }

        private VaryingDecl ParseVaryingDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            tokens.Expect(ValkyrieTokenKind.Colon);
            var type = tokens.ParseType();
            string? interpolation = null;

            tokens.Match(ValkyrieTokenKind.Semicolon);
            tokens.Match(ValkyrieTokenKind.Comma);
            return new VaryingDecl
                { Name = name, VaryingType = type, Interpolation = interpolation, Attributes = attrs };
        }

        private ConstantBufferDecl ParseConstantBufferDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            tokens.Expect(ValkyrieTokenKind.BraceL);
            var fields = new List<DeclareObjectField>();
            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                if (tokens.PeekKind() == ValkyrieTokenKind.Identifier)
                {
                    fields.Add(tokens.ParseField());
                }
                else
                {
                    tokens.SkipUnrecognizedToken();
                }
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);
            int? group = null;
            int? binding = null;
            if (tokens.Check(ValkyrieTokenKind.Identifier) && tokens.PeekText() == "group")
            {
                tokens.Advance();
                group = tokens.SafeParseInt(tokens.AdvanceText());
            }

            if (tokens.Check(ValkyrieTokenKind.Binding))
            {
                tokens.Advance();
                binding = tokens.SafeParseInt(tokens.AdvanceText());
            }
            else if (tokens.Check(ValkyrieTokenKind.Identifier) && tokens.PeekText() == "binding")
            {
                tokens.Advance();
                binding = tokens.SafeParseInt(tokens.AdvanceText());
            }

            tokens.Match(ValkyrieTokenKind.Semicolon);
            tokens.Match(ValkyrieTokenKind.Comma);
            return new ConstantBufferDecl
                { Name = name, Fields = fields, Group = group, Binding = binding, Attributes = attrs };
        }

        private TextureDecl ParseTextureDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            tokens.Expect(ValkyrieTokenKind.Colon);
            var type = tokens.ParseType();
            int? group = null;
            int? binding = null;
            if (tokens.Check(ValkyrieTokenKind.Identifier) && tokens.PeekText() == "group")
            {
                tokens.Advance();
                group = tokens.SafeParseInt(tokens.AdvanceText());
            }

            if (tokens.Check(ValkyrieTokenKind.Binding))
            {
                tokens.Advance();
                binding = tokens.SafeParseInt(tokens.AdvanceText());
            }
            else if (tokens.Check(ValkyrieTokenKind.Identifier) && tokens.PeekText() == "binding")
            {
                tokens.Advance();
                binding = tokens.SafeParseInt(tokens.AdvanceText());
            }

            tokens.Match(ValkyrieTokenKind.Semicolon);
            tokens.Match(ValkyrieTokenKind.Comma);
            return new TextureDecl
                { Name = name, TextureType = type, Group = group, Binding = binding, Attributes = attrs };
        }

        private SamplerDecl ParseSamplerDecl()
        {
            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();
            int? group = null;
            int? binding = null;
            if (tokens.Check(ValkyrieTokenKind.Identifier) && tokens.PeekText() == "group")
            {
                tokens.Advance();
                group = tokens.SafeParseInt(tokens.AdvanceText());
            }

            if (tokens.Check(ValkyrieTokenKind.Binding))
            {
                tokens.Advance();
                binding = tokens.SafeParseInt(tokens.AdvanceText());
            }
            else if (tokens.Check(ValkyrieTokenKind.Identifier) && tokens.PeekText() == "binding")
            {
                tokens.Advance();
                binding = tokens.SafeParseInt(tokens.AdvanceText());
            }

            tokens.Match(ValkyrieTokenKind.Semicolon);
            tokens.Match(ValkyrieTokenKind.Comma);
            return new SamplerDecl { Name = name, Group = group, Binding = binding, Attributes = attrs };
        }

        internal DeclareLet ParseVariableDecl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var isMutable = false;

            if (tokens.Check(ValkyrieTokenKind.Identifier)
                && string.Equals(tokens.PeekText(), "mut", StringComparison.OrdinalIgnoreCase))
            {
                tokens.Advance();
                isMutable = true;
            }

            var name = tokens.AdvanceText();
            var attrs = tokens.ParseAttributes();

            TypeNode? type = null;
            ValkyrieNode? init = null;

            if (tokens.Check(ValkyrieTokenKind.Colon))
            {
                tokens.Advance();
                type = tokens.ParseType();
            }

            if (tokens.Check(ValkyrieTokenKind.Equal))
            {
                tokens.Advance();
                init = tokens.ParseExpressionNode();
            }

            tokens.Match(ValkyrieTokenKind.Semicolon);
            tokens.Match(ValkyrieTokenKind.Comma);

            return new DeclareLet
            {
                Name = new IdentifierNode(name),
                IsMutable = isMutable,
                Modifiers = modifiers,
                Attributes = attrs,
                VarType = type,
                Initializer = init
            };
        }

        private ValkyrieNode SkipUnknownDecl()
        {
            if (tokens.Check(ValkyrieTokenKind.BraceR) || tokens.Check(ValkyrieTokenKind.Semicolon))
            {
                return new UnknownDecl { Content = "" };
            }

            var content = tokens.AdvanceText();
            while (!tokens.IsAtEnd()
                   && !tokens.Check(ValkyrieTokenKind.Semicolon)
                   && !tokens.Check(ValkyrieTokenKind.BraceR))
            {
                content += " " + tokens.AdvanceText();
            }

            tokens.Match(ValkyrieTokenKind.Semicolon);
            tokens.Diagnostics?.AddWarning("", new TextSpan(tokens.Position, 1), "PARSE", $"无法识别的声明：{content}");
            return new UnknownDecl { Content = content };
        }

        private ChannelConditionalDecl ParseChannelConditionalDecl(ValkyrieLanguage language)
        {
            tokens.Expect(ValkyrieTokenKind.ParenthesisL);
            var channelName = tokens.AdvanceText();
            tokens.Expect(ValkyrieTokenKind.ParenthesisR);
            tokens.Expect(ValkyrieTokenKind.BraceL);

            var decls = new List<ValkyrieNode>();

            while (!tokens.Check(ValkyrieTokenKind.BraceR) && !tokens.IsAtEnd())
            {
                var leadingAttrs = tokens.ParseAttributes();
                var leadingMods = tokens.CollectLeadingModifiers();
                var leadingDocs = tokens.ParseDocComments();

                if (tokens.IsDeclarationStart(language))
                {
                    decls.Add(tokens.ParseDeclarationWithModifiers(language, leadingAttrs, leadingMods, leadingDocs));
                }
                else
                {
                    decls.Add(tokens.ParseStatementNode(language));
                }
            }

            tokens.Expect(ValkyrieTokenKind.BraceR);

            return new ChannelConditionalDecl
            {
                ChannelName = channelName,
                Declarations = decls
            };
        }

        private bool IsFieldStart()
        {
            var kind = tokens.PeekKind();
            return kind is ValkyrieTokenKind.Identifier or ValkyrieTokenKind.BracketL;
        }
    }

    private static readonly HashSet<string> s_bodyDeclarationKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "meta"
    };

    extension(TokenStream tokens)
    {
        private bool IsFunctionDeclFollows()
        {
            return tokens.CountLeadingFunctionModifiers() >= 0;
        }

        private int CountLeadingFunctionModifiers()
        {
            var count = 0;

            var firstToken = tokens.Peek(count);
            if (firstToken.Kind.IsKeyword() || firstToken.Kind == ValkyrieTokenKind.Identifier.ToNodeKind())
            {
                if (s_bodyDeclarationKeywords.Contains(tokens.PeekText(count)))
                {
                    return -1;
                }

                if (firstToken.Kind.IsKeyword() && IsShaderStageKeyword(tokens.PeekText(count)))
                {
                    return -1;
                }
            }

            while (true)
            {
                var kind = tokens.Peek(count).Kind;
                if (kind != ValkyrieTokenKind.Identifier.ToNodeKind() && !kind.IsKeyword())
                {
                    return -1;
                }

                count++;
                var next = tokens.Peek(count);
                if (next.Kind == ValkyrieTokenKind.ParenthesisL.ToNodeKind())
                {
                    return count;
                }

                if (next.Kind != ValkyrieTokenKind.Identifier.ToNodeKind() && !next.Kind.IsKeyword())
                {
                    return -1;
                }
            }
        }

        private IReadOnlyList<string> CollectBlockFunctionModifiers()
        {
            var count = tokens.CountLeadingFunctionModifiers();
            if (count <= 0)
            {
                return [];
            }

            var mods = new List<string>(count);
            for (var i = 0; i < count - 1; i++)
            {
                mods.Add(tokens.AdvanceText());
            }

            return mods;
        }
    }

    private static bool IsShaderStageKeyword(string text)
    {
        return text is "vertex" or "fragment" or "compute"
            or "raygen" or "closesthit" or "anyhit" or "miss";
    }

    extension(TokenStream tokens)
    {
        private DeclareObjectField ParseField()
        {
            var fieldAttrs = new List<AttributeItem>();
            fieldAttrs.AddRange(tokens.ParseAttributes());

            var name = tokens.AdvanceText();

            var doc = tokens.ParseDocComments();

            tokens.Expect(ValkyrieTokenKind.Colon);
            var type = tokens.ParseType();

            ValkyrieNode? defaultVal = null;
            if (tokens.Check(ValkyrieTokenKind.Equal))
            {
                tokens.Advance();
                defaultVal = tokens.ParseExpressionNode();
            }

            tokens.Match(ValkyrieTokenKind.Semicolon);
            tokens.Match(ValkyrieTokenKind.Comma);

            return new DeclareObjectField
            {
                Name = name,
                FieldType = type,
                DocComments = doc,
                DefaultValue = defaultVal,
                Attributes = fieldAttrs
            };
        }

        internal TypeNode ParseType()
        {
            if (tokens.Check(ValkyrieTokenKind.Amp))
            {
                tokens.Advance();
                var refType = tokens.ParseType();
                return new TypeNode("&", [refType]);
            }

            if (tokens.Check(ValkyrieTokenKind.ParenthesisL))
            {
                tokens.Advance();
                var elements = new List<TypeNode>();
                while (!tokens.Check(ValkyrieTokenKind.ParenthesisR) && !tokens.IsAtEnd())
                {
                    elements.Add(tokens.ParseType());
                    tokens.Match(ValkyrieTokenKind.Comma);
                }

                tokens.Expect(ValkyrieTokenKind.ParenthesisR);
                if (elements.Count == 1)
                {
                    return elements[0];
                }

                return new TypeNode("tuple", elements);
            }

            if (tokens.Check(ValkyrieTokenKind.BracketL))
            {
                tokens.Advance();
                var elemType = tokens.ParseType();
                tokens.Expect(ValkyrieTokenKind.BracketR);
                return new TypeNode("list", [elemType]);
            }

            var name = tokens.AdvanceText();
            var genericArgs = new List<TypeNode>();

            if (tokens.Check(ValkyrieTokenKind.Less))
            {
                tokens.Advance();
                while (!tokens.IsAtEnd()
                       && !(tokens.Check(ValkyrieTokenKind.Greater)))
                {
                    genericArgs.Add(tokens.ParseType());
                    tokens.Match(ValkyrieTokenKind.Comma);
                }

                tokens.Advance();
            }

            if (tokens.Check(ValkyrieTokenKind.ParenthesisL))
            {
                tokens.Advance();
                while (!tokens.IsAtEnd() && !tokens.Check(ValkyrieTokenKind.ParenthesisR))
                {
                    genericArgs.Add(tokens.ParseType());
                    tokens.Match(ValkyrieTokenKind.Comma);
                }

                tokens.Expect(ValkyrieTokenKind.ParenthesisR);
            }

            if (tokens.Check(ValkyrieTokenKind.ParenthesisL))
            {
                tokens.Advance();
                while (!tokens.IsAtEnd() && !tokens.Check(ValkyrieTokenKind.ParenthesisR))
                {
                    genericArgs.Add(tokens.ParseType());
                    tokens.Match(ValkyrieTokenKind.Comma);
                }

                tokens.Expect(ValkyrieTokenKind.ParenthesisR);
            }

            var typeNode = new TypeNode(name, genericArgs);

            if (tokens.Check(ValkyrieTokenKind.Question))
            {
                tokens.Advance();
                return new TypeUnaryExpression("?", typeNode, false);
            }

            return typeNode;
        }

        private QueryExpr ParseQueryExpr()
        {
            tokens.Advance();

            if (tokens.Check(ValkyrieTokenKind.Equal))
            {
                tokens.Advance();
            }

            var result = tokens.ParseQueryBody();

            return result;
        }

        private QueryKind ParseQueryKind()
        {
            if (tokens.Check(ValkyrieTokenKind.Dot))
            {
                tokens.Advance();
                var method = tokens.AdvanceText().ToLowerInvariant();
                return method switch
                {
                    "all" => QueryKind.All,
                    "any" => QueryKind.Any,
                    "none" => QueryKind.None,
                    _ => QueryKind.All
                };
            }

            return QueryKind.All;
        }

        private QueryExpr ParseQueryBody()
        {
            var calleeText = tokens.AdvanceText();

            var kind = tokens.ParseQueryKind();

            tokens.Expect(ValkyrieTokenKind.ParenthesisL);
            var comps = new List<TypeNode>();

            while (!tokens.Check(ValkyrieTokenKind.ParenthesisR) && !tokens.IsAtEnd())
            {
                comps.Add(tokens.ParseType());
                tokens.Match(ValkyrieTokenKind.Comma);
            }

            tokens.Expect(ValkyrieTokenKind.ParenthesisR);

            var filters = new List<QueryExpr>();
            while (tokens.Check(ValkyrieTokenKind.Dot))
            {
                tokens.Advance();
                break;
            }

            return new QueryExpr
            {
                Kind = kind,
                ComponentTypes = comps,
                Filters = filters.Count > 0 ? filters : null
            };
        }

        private QueryExpr ParseNamedQueryExpr()
        {
            tokens.Advance();
            tokens.Advance();
            return tokens.ParseQueryBody();
        }

        private List<ParameterList> ParseParamList()
        {
            var list = new List<ParameterList>();
            while (!tokens.Check(ValkyrieTokenKind.ParenthesisR) && !tokens.IsAtEnd())
            {
                var name = tokens.AdvanceText();
                TypeNode? type = null;
                if (tokens.Check(ValkyrieTokenKind.Colon))
                {
                    tokens.Advance();
                    type = tokens.ParseType();
                }

                list.Add(new ParameterList
                {
                    Name = new IdentifierNode(name),
                    ParamType = type ?? new TypeNode { Name = "any" }
                });
                tokens.Match(ValkyrieTokenKind.Comma);
            }

            return list;
        }

        internal IReadOnlyList<AttributeItem> ParseAttributes()
        {
            var attrs = new List<AttributeItem>();

            while (tokens.Check(ValkyrieTokenKind.BracketL))
            {
                var nextKind = tokens.Peek(1).Kind;
                if (nextKind != ValkyrieTokenKind.Identifier.ToNodeKind() && !nextKind.IsKeyword())
                {
                    break;
                }

                tokens.Advance();

                var contentBuilder = new StringBuilder();
                var depth = 1;
                while (depth > 0 && !tokens.IsAtEnd())
                {
                    if (tokens.Check(ValkyrieTokenKind.BracketL))
                    {
                        depth++;
                        contentBuilder.Append(tokens.AdvanceText());
                    }
                    else if (tokens.Check(ValkyrieTokenKind.BracketR))
                    {
                        depth--;
                        if (depth > 0)
                        {
                            contentBuilder.Append(tokens.AdvanceText());
                        }
                        else
                        {
                            tokens.Advance();
                        }
                    }
                    else
                    {
                        contentBuilder.Append(tokens.AdvanceText());
                    }
                }

                var content = contentBuilder.ToString().Trim();
                var parts = SplitAttributeParts(content);
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                    {
                        continue;
                    }

                    attrs.Add(ParseSingleAttribute(trimmed));
                }
            }

            return attrs;
        }
    }

    private static List<string> SplitAttributeParts(string content)
    {
        var parts = new List<string>();
        var parenDepth = 0;
        var inString = false;
        var start = 0;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            if (c == '"' && (i == 0 || content[i - 1] != '\\'))
            {
                inString = !inString;
            }
            else if (!inString)
            {
                if (c == '(')
                {
                    parenDepth++;
                }
                else if (c == ')')
                {
                    parenDepth--;
                }
                else if (c == ',' && parenDepth == 0)
                {
                    parts.Add(content[start..i]);
                    start = i + 1;
                }
            }
        }

        if (start < content.Length)
        {
            parts.Add(content[start..]);
        }

        return parts;
    }

    private static AttributeItem ParseSingleAttribute(string text)
    {
        var parenIndex = text.IndexOf('(');
        if (parenIndex < 0)
        {
            return new AttributeItem { Name = text };
        }

        var name = text[..parenIndex].Trim();
        var lastParen = text.LastIndexOf(')');
        if (lastParen <= parenIndex)
        {
            return new AttributeItem { Name = text };
        }

        var argsContent = text[(parenIndex + 1)..lastParen];

        var argParts = SplitAttributeParts(argsContent);
        var arguments = new List<ArgumentItem>();
        foreach (var arg in argParts)
        {
            var value = arg.Trim().Trim('"');
            arguments.Add(new ArgumentItem
            {
                Value = new TermNode
                {
                    Expression = new TermAtomicLiteral(LiteralType.String, value)
                }
            });
        }

        return new AttributeItem
        {
            Name = name,
            Arguments = new ArgumentList
            {
                Items = arguments
            }
        };
    }

    extension(TokenStream tokens)
    {
        internal IReadOnlyList<DocumentComment> ParseDocComments()
        {
            var docs = new List<DocumentComment>();
            while (tokens.Check(ValkyrieTokenKind.CommentStart))
            {
                tokens.Advance();
                if (tokens.Check(ValkyrieTokenKind.CommentContent))
                {
                    docs.Add(new DocumentComment { Content = tokens.AdvanceText() });
                }
            }

            return docs;
        }

        private string ParseDottedName()
        {
            var name = tokens.AdvanceText();
            while (tokens.Check(ValkyrieTokenKind.Dot))
            {
                tokens.Advance();
                name += "." + tokens.AdvanceText();
            }

            return name;
        }

        private IReadOnlyList<string> ParseModifiers()
        {
            var mods = new List<string>();
            while (!tokens.IsAtEnd())
            {
                var kind = tokens.Peek(0).Kind;
                var nextKind = tokens.Peek(1).Kind;

                if (kind == ValkyrieTokenKind.Identifier.ToNodeKind()
                    && (nextKind == ValkyrieTokenKind.Identifier.ToNodeKind() || nextKind.IsKeyword()))
                {
                    mods.Add(tokens.AdvanceText());
                }
                else
                {
                    break;
                }
            }

            return mods;
        }

        private IReadOnlyList<TypeParameter> ParseTypeParameters()
        {
            if (!tokens.Check(ValkyrieTokenKind.Less))
            {
                return [];
            }

            tokens.Advance();
            var typeParams = new List<TypeParameter>();

            while (!tokens.IsAtEnd()
                   && !tokens.Check(ValkyrieTokenKind.Greater))
            {
                var name = tokens.AdvanceText();
                typeParams.Add(new TypeParameter { Name = name });
                tokens.Match(ValkyrieTokenKind.Comma);
            }

            tokens.Advance();
            return typeParams;
        }

        private IReadOnlyList<GenericConstraint> ParseGenericConstraints()
        {
            var constraints = new List<GenericConstraint>();

            while (tokens.Check(ValkyrieTokenKind.Where))
            {
                tokens.Advance();
                var parameterName = tokens.AdvanceText();
                tokens.Expect(ValkyrieTokenKind.Colon);
                var constraintTypes = new List<TypeNode> { tokens.ParseType() };

                while (tokens.Check(ValkyrieTokenKind.Comma))
                {
                    tokens.Advance();
                    constraintTypes.Add(tokens.ParseType());
                }

                constraints.Add(new GenericConstraint
                {
                    ParameterName = parameterName,
                    ConstraintTypes = constraintTypes
                });
            }

            return constraints;
        }

        private int SafeParseInt(string text)
        {
            if (int.TryParse(text, out var result))
            {
                return result;
            }

            tokens.Diagnostics?.AddError(
                "", new TextSpan(tokens.Position, 1), "PARSE", $"无法将 \"{text}\" 解析为整数");
            return 0;
        }

        private void SkipUnrecognizedToken()
        {
            var text = tokens.PeekText();
            tokens.Diagnostics?.AddWarning(
                "", new TextSpan(tokens.Position, 1), "PARSE", $"块内无法识别的元素：\"{text}\"");
            tokens.Advance();
        }
    }
}
