using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.ECS;
using Oak.Valkyrie.AST.Neural;
using Oak.Valkyrie.AST.Schema;
using Oak.Valkyrie.AST.Shader;
using Oak.Valkyrie.AST.Statement;
using Oak.Valkyrie.AST.Template;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.Visitor;

/// <summary>
///     Valkyrie AST 类型化访问器抽象基类
///     为每种 AST 节点类型提供独立的 Visit 方法，
///     支持深度优先遍历
/// </summary>
public abstract class ValkyrieAstVisitor
{
    public virtual void Visit(ValkyrieNode node)
    {
        switch (node)
        {
            case ProgramRoot n: VisitProgramRoot(n); break;
            case DeclareLet n: VisitLetDecl(n); break;
            case DeclareMicro n: VisitFunctionDecl(n); break;
            case DeclareComponent n: VisitComponentDecl(n); break;
            case DeclareSystem n: VisitSystemDecl(n); break;
            case DeclareWidget n: VisitWidgetDecl(n); break;
            case DeclareStructure n: VisitStructDecl(n); break;
            case DeclareObjectField n: VisitFieldDecl(n); break;
            case ObjectBody n: VisitObjectBody(n); break;
            case DeclareObjectMethod n: VisitObjectMethod(n); break;
            case ParameterList n: VisitParameterDecl(n); break;
            case TypeNode n: VisitTypeAnnotation(n); break;
            case AttributeItem n: VisitAttributeDecl(n); break;
            case DocumentComment n: VisitDocCommentDecl(n); break;
            case DeclareNamespace n: VisitNamespaceDecl(n); break;
            case DeclareClass n: VisitClassDecl(n); break;
            case InheritanceList n: VisitInheritanceSpec(n); break;
            case DeclareObjectDomain n: VisitDomainDecl(n); break;
            case DeclareEnums n: VisitEnumDecl(n); break;
            case DeclareSemanticMember n: VisitEnumMemberDecl(n); break;
            case DeclareFlags n: VisitFlagsDecl(n); break;
            case DeclareUnite n: VisitUniteDecl(n); break;
            case DeclareUniteVariant n: VisitUnionVariantDecl(n); break;
            case DeclareTraitAlias n: VisitTypeAliasDecl(n); break;
            case DeclareModel n: VisitModelDecl(n); break;
            case DeclareService n: VisitServiceDecl(n); break;
            case DeclareUsing n: VisitUsingDecl(n); break;
            case UniformBindingDecl n: VisitUniformBindingDecl(n); break;
            case NeuralDecl n: VisitNeuralDecl(n); break;
            case TensorTypeExpr n: VisitTensorTypeExpr(n); break;
            case FunctionBody n: VisitBlockStmt(n); break;
            case IfStatement n: VisitIfStmt(n); break;
            case WhileStatement n: VisitWhileStmt(n); break;
            case LoopStatement n: VisitLoopStatement(n); break;
            case MatchStatement n: VisitMatchStmt(n); break;
            case MatchArm n: VisitMatchArm(n); break;
            case CatchStatement n: VisitCatchStmt(n); break;
            case CatchArm n: VisitCatchArm(n); break;
            case ReturnStatement n: VisitReturnStmt(n); break;
            case ResumeStatement n: VisitResumeStmt(n); break;
            case LetStatement n: VisitDiscardStmt(n); break;
            case TermBinaryExpression n: VisitBinaryExpr(n); break;
            case AssignmentExpr n: VisitAssignmentExpr(n); break;
            case TermUnaryExpression n: VisitUnaryExpr(n); break;
            case TermCallExpression n: VisitCallExpr(n); break;
            case TermOrdinalExpression n: VisitOrdinalIndexExpr(n); break;
            case TermOffsetExpression n: VisitOffsetIndexExpr(n); break;
            case TermDotExpression n: VisitMemberAccessExpr(n); break;
            case QualifiedPath n: VisitQualifiedPathExpr(n); break;
            case TermAtomicLiteral n: VisitLiteralExpr(n); break;
            case IdentifierNode n: VisitIdentifierNode(n); break;
            case AnonymousMicro n: VisitLambdaExpr(n); break;
            case QueryExpr n: VisitQueryExpr(n); break;
         
            case SwizzleExpr n: VisitSwizzleExpr(n); break;
            case TermNode n: VisitExprStmt(n); break;
            case ShaderDecl n: VisitShaderDecl(n); break;
            case VertexShaderDecl n: VisitVertexShaderDecl(n); break;
            case FragmentShaderDecl n: VisitFragmentShaderDecl(n); break;
            case ComputeShaderDecl n: VisitComputeShaderDecl(n); break;
            case UniformDecl n: VisitUniformDecl(n); break;
            case VaryingDecl n: VisitVaryingDecl(n); break;
            case ConstantBufferDecl n: VisitConstantBufferDecl(n); break;
            case TextureDecl n: VisitTextureDecl(n); break;
            case SamplerDecl n: VisitSamplerDecl(n); break;
            case ShaderAttributeDecl n: VisitShaderAttributeDecl(n); break;
            default: VisitDefault(node); break;
        }
    }

    public virtual void VisitDefault(ValkyrieNode node)
    {
    }

    #region 声明节点

    public virtual void VisitProgramRoot(ProgramRoot node)
    {
        foreach (var decl in node.Declarations)
        {
            Visit(decl);
        }
    }

    public virtual void VisitLetDecl(DeclareLet node)
    {
        if (node.Initializer is not null)
        {
            Visit(node.Initializer);
        }

        foreach (var attr in node.Attributes)
        {
            Visit(attr);
        }
    }

    public virtual void VisitFunctionDecl(DeclareMicro node)
    {
        foreach (var param in node.Parameters)
        {
            Visit(param);
        }

        if (node.ReturnType is not null)
        {
            Visit(node.ReturnType);
        }

        if (node.Body is not null)
        {
            Visit(node.Body);
        }

        foreach (var attr in node.Annotations.Attributes())
        {
            Visit(attr);
        }
    }

    public virtual void VisitComponentDecl(DeclareComponent node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }

        foreach (var attr in node.Annotations.Attributes())
        {
            Visit(attr);
        }
    }

    public virtual void VisitSystemDecl(DeclareSystem node)
    {
        foreach (var query in node.Queries)
        {
            Visit(query);
        }

        if (node.Body is not null)
        {
            Visit(node.Body);
        }

        foreach (var attr in node.Annotations.Attributes())
        {
            Visit(attr);
        }
    }

    public virtual void VisitWidgetDecl(DeclareWidget node)
    {
        foreach (var prop in node.Properties)
        {
            Visit(prop);
        }

        if (node.RenderMethod is not null)
        {
            Visit(node.RenderMethod);
        }

        foreach (var attr in node.Annotations.Attributes())
        {
            Visit(attr);
        }
    }

    public virtual void VisitStructDecl(DeclareStructure node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }

        foreach (var attr in node.Annotations.Attributes())
        {
            Visit(attr);
        }
    }

    public virtual void VisitFieldDecl(DeclareObjectField node)
    {
        Visit(node.FieldType);

        if (node.DefaultValue is not null)
        {
            Visit(node.DefaultValue);
        }

        foreach (var attr in node.Attributes)
        {
            Visit(attr);
        }
    }

    public virtual void VisitParameterDecl(ParameterList node)
    {
        Visit(node.ParamType);

        foreach (var attr in node.Annotations.Attributes())
        {
            Visit(attr);
        }
    }

    public virtual void VisitTypeAnnotation(TypeNode node)
    {
        foreach (var arg in node.GenericArgs)
        {
            Visit(arg);
        }
    }

    public virtual void VisitAttributeDecl(AttributeItem node)
    {
    }

    public virtual void VisitDocCommentDecl(DocumentComment node)
    {
    }

    public virtual void VisitNamespaceDecl(DeclareNamespace node)
    {
        foreach (var decl in node.Declarations)
        {
            Visit(decl);
        }
    }

    public virtual void VisitClassDecl(DeclareClass node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }

        foreach (var attr in node.Annotations.Attributes())
        {
            Visit(attr);
        }

    }

    public virtual void VisitInheritanceSpec(InheritanceList list)
    {
        foreach (var baseItem in list.Bases)
        {
            Visit(baseItem);
        }
    }

    public virtual void VisitDomainDecl(DeclareObjectDomain node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }

        foreach (var attr in node.Attributes)
        {
            Visit(attr);
        }
    }

    public virtual void VisitEnumDecl(DeclareEnums node)
    {
        foreach (var member in node.Members)
        {
            Visit(member);
        }
    }

    public virtual void VisitEnumMemberDecl(DeclareSemanticMember node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitFlagsDecl(DeclareFlags node)
    {
        foreach (var member in node.Members)
        {
            Visit(member);
        }
    }

    public virtual void VisitUnionDecl(DeclareUnite node)
    {
        foreach (var variant in node.Variants)
        {
            Visit(variant);
        }
    }

    public virtual void VisitUnionVariantDecl(DeclareUniteVariant node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }
    }

    public virtual void VisitUniteDecl(DeclareUnite node)
    {
    }

    public virtual void VisitTypeAliasDecl(DeclareTraitAlias node)
    {
        Visit(node.TargetType);
    }

    public virtual void VisitModelDecl(DeclareModel node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }
    }

    public virtual void VisitObjectBody(ObjectBody node)
    {
        foreach (var field in node.Fields)
        {
            Visit(field);
        }

        foreach (var domain in node.Domains)
        {
            Visit(domain);
        }

        foreach (var method in node.Methods)
        {
            Visit(method);
        }
    }

    public virtual void VisitObjectMethod(DeclareObjectMethod node)
    {
        foreach (var attr in node.Annotations.Attributes())
        {
            Visit(attr);
        }
    }

    public virtual void VisitServiceDecl(DeclareService node)
    {
    }

    public virtual void VisitUsingDecl(DeclareUsing node)
    {
    }

    public virtual void VisitUniformBindingDecl(UniformBindingDecl node)
    {
        Visit(node.BindingType);
    }

    public virtual void VisitNeuralDecl(NeuralDecl node)
    {
        foreach (var layer in node.Layers)
        {
            Visit(layer);
        }
    }

    public virtual void VisitTensorTypeExpr(TensorTypeExpr node)
    {
    }

    #endregion

    #region 语句节点

    public virtual void VisitBlockStmt(FunctionBody node)
    {
        foreach (var stmt in node.Statements)
        {
            Visit(stmt);
        }
    }

    public virtual void VisitIfStmt(IfStatement node)
    {
        Visit(node.Condition);
        Visit(node.ThenBlock);
        if (node.ElseBlock is not null)
        {
            Visit(node.ElseBlock);
        }
    }

    public virtual void VisitWhileStmt(WhileStatement node)
    {
        Visit(node.Condition);
        Visit(node.Body);
    }

    public virtual void VisitLoopStatement(LoopStatement node)
    {
        Visit(node.Body);
    }

    public virtual void VisitMatchStmt(MatchStatement node)
    {
        foreach (var arm in node.Arms)
        {
            Visit(arm);
        }
    }

    public virtual void VisitMatchArm(MatchArm node)
    {
        Visit(node.Pattern);

        if (node.Body is not null)
        {
            Visit(node.Body);
        }
    }

    public virtual void VisitCatchStmt(CatchStatement node)
    {
        foreach (var arm in node.Arms)
        {
            Visit(arm);
        }
    }

    public virtual void VisitCatchArm(CatchArm node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }
    }

    public virtual void VisitReturnStmt(ReturnStatement node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitResumeStmt(ResumeStatement node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitDiscardStmt(LetStatement node)
    {
    }

    #endregion

    #region 表达式节点

    public virtual void VisitBinaryExpr(TermBinaryExpression node)
    {
        Visit(node.Left);
        Visit(node.Right);
    }

    public virtual void VisitAssignmentExpr(AssignmentExpr node)
    {
        Visit(node.Target);
        Visit(node.Value);
    }

    public virtual void VisitUnaryExpr(TermUnaryExpression node)
    {
        Visit(node.Operand);
    }

    public virtual void VisitCallExpr(TermCallExpression node)
    {
        Visit(node.Callee);

        foreach (var arg in node.Arguments)
        {
            Visit(arg);
        }
    }

    public virtual void VisitIndexExpr(TermOffsetExpression node)
    {
        Visit(node.Target);
    }

    public virtual void VisitOrdinalIndexExpr(TermOrdinalExpression node)
    {
        Visit(node.Target);

        foreach (var index in node.Indices)
        {
            Visit(index);
        }
    }

    public virtual void VisitOffsetIndexExpr(TermOffsetExpression node)
    {
        Visit(node.Target);

        foreach (var index in node.Indices)
        {
            Visit(index);
        }
    }

    public virtual void VisitMemberAccessExpr(TermDotExpression node)
    {
        Visit(node.Target);
    }

    public virtual void VisitQualifiedPathExpr(QualifiedPath node)
    {
    }

    public virtual void VisitLiteralExpr(TermAtomicLiteral node)
    {
    }

    public virtual void VisitIdentifierNode(IdentifierNode node)
    {
    }

    public virtual void VisitLambdaExpr(AnonymousMicro node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }
    }

    public virtual void VisitQueryExpr(QueryExpr node)
    {
        if (node.Filters is not null)
        {
            foreach (var clause in node.Filters)
            {
                Visit(clause);
            }
        }
    }



    public virtual void VisitSwizzleExpr(SwizzleExpr node)
    {
        Visit(node.Target);
    }

    public virtual void VisitExprStmt(TermNode node)
    {
        Visit(node.Expression);
    }

    #endregion

    #region Shader 节点

    public virtual void VisitShaderDecl(ShaderDecl node)
    {
        foreach (var attr in node.Annotations.Attributes())
        {
            Visit(attr);
        }
    }

    public virtual void VisitVertexShaderDecl(VertexShaderDecl node)
    {
        foreach (var attr in node.Attributes)
        {
            Visit(attr);
        }
    }

    public virtual void VisitFragmentShaderDecl(FragmentShaderDecl node)
    {
        foreach (var attr in node.Attributes)
        {
            Visit(attr);
        }
    }

    public virtual void VisitComputeShaderDecl(ComputeShaderDecl node)
    {
        foreach (var attr in node.Attributes)
        {
            Visit(attr);
        }
    }

    public virtual void VisitUniformDecl(UniformDecl node)
    {
        Visit(node.UniformType);
    }

    public virtual void VisitVaryingDecl(VaryingDecl node)
    {
        Visit(node.VaryingType);
    }

    public virtual void VisitConstantBufferDecl(ConstantBufferDecl node)
    {
        foreach (var field in node.Fields)
        {
            Visit(field);
        }
    }

    public virtual void VisitTextureDecl(TextureDecl node)
    {
        Visit(node.TextureType);
    }

    public virtual void VisitSamplerDecl(SamplerDecl node)
    {
    }

    public virtual void VisitShaderAttributeDecl(ShaderAttributeDecl node)
    {
    }

    #endregion
}
