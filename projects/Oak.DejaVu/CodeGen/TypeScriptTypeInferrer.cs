using System.Text;
using Oak.DejaVu.Expressions;
using Oak.DejaVu.Optimizer;

namespace Oak.DejaVu.CodeGen;

/// <summary>
///     TypeScript 类型推导器——从模板 AST 推导所需的 Data 接口。
///     输出 TypeScript 接口定义，用于编译期类型检查。
/// </summary>
public sealed class TypeScriptTypeInferrer
{
    /// <summary>
    ///     从模板节点推导 TypeScript Data 接口
    /// </summary>
    /// <param name="nodes">优化后的模板节点</param>
    /// <param name="interfaceName">接口名称</param>
    /// <returns>TypeScript 接口源码</returns>
    public string InferInterface(IReadOnlyList<DejaVuTemplateNode> nodes, string interfaceName = "TemplateData")
    {
        var fields = new Dictionary<string, InferredType>();

        CollectFields(nodes, fields);

        return GenerateInterface(fields, interfaceName);
    }

    /// <summary>
    ///     从符号表推导 TypeScript Data 接口
    /// </summary>
    /// <param name="symbolTable">编译期符号表</param>
    /// <param name="interfaceName">接口名称</param>
    /// <returns>TypeScript 接口源码</returns>
    public string InferFromSymbolTable(SymbolTable symbolTable, string interfaceName = "TemplateData")
    {
        var fields = new Dictionary<string, InferredType>();

        foreach (var scope in symbolTable.AllScopes)
        {
            foreach (var (name, isDeclared) in scope.References)
            {
                if (!isDeclared && !fields.ContainsKey(name))
                {
                    fields[name] = new InferredType("any", false);
                }
            }
        }

        return GenerateInterface(fields, interfaceName);
    }

    private void CollectFields(IReadOnlyList<DejaVuTemplateNode> nodes, Dictionary<string, InferredType> fields)
    {
        foreach (var node in nodes)
        {
            CollectFieldsFromNode(node, fields);
        }
    }

    private void CollectFieldsFromNode(DejaVuTemplateNode node, Dictionary<string, InferredType> fields)
    {
        switch (node)
        {
            case DejaVuCodeNode codeNode:
                CollectFromExpression(codeNode.ParsedExpression, fields);
                break;
            case DejaVuIfNode ifNode:
                CollectFromExpression(ifNode.ParsedCondition, fields);
                CollectFields(ifNode.Children, fields);
                foreach (var elseIf in ifNode.ElseIfNodes)
                {
                    CollectFromExpression(elseIf.ParsedCondition, fields);
                    CollectFields(elseIf.Children, fields);
                }

                CollectFields(ifNode.ElseChildren, fields);
                break;
            case DejaVuLoopNode loopNode:
                CollectFromExpression(loopNode.ParsedExpression, fields);
                if (loopNode.ParsedExpression is IdentifierNode idNode)
                {
                    MergeField(fields, idNode.Name, new InferredType("any[]", true));
                }
                else if (loopNode.ParsedExpression is MemberAccessNode { Object: IdentifierNode memberIdNode } memberNode)
                {
                    MergeField(fields, memberIdNode.Name, new InferredType($"{{ {memberNode.MemberName}: any[] }}", false));
                }

                CollectFields(loopNode.Children, fields);
                break;
            case DejaVuLetNode letNode:
                CollectFromExpression(letNode.ParsedExpression, fields);
                CollectFields(letNode.Children, fields);
                break;
            case DejaVuWithNode withNode:
                CollectFromExpression(withNode.ParsedExpression, fields);
                CollectFields(withNode.Children, fields);
                break;
            case DejaVuBlockNode blockNode:
                CollectFields(blockNode.Children, fields);
                break;
            case DejaVuMatchNode matchNode:
                CollectFromExpression(matchNode.ParsedExpression, fields);
                CollectFields(matchNode.Children, fields);
                break;
            case DejaVuRawNode rawNode:
                CollectFields(rawNode.Children, fields);
                break;
        }
    }

    private void CollectFromExpression(IExpressionNode? node, Dictionary<string, InferredType> fields)
    {
        if (node == null) return;

        switch (node)
        {
            case IdentifierNode id:
                MergeField(fields, id.Name, new InferredType("any", false));
                break;
            case BinaryNode binary:
                CollectFromExpression(binary.Left, fields);
                CollectFromExpression(binary.Right, fields);
                break;
            case UnaryNode unary:
                CollectFromExpression(unary.Operand, fields);
                break;
            case MemberAccessNode member:
                CollectFromExpression(member.Object, fields);
                if (member.Object is IdentifierNode parentId)
                {
                    MergeField(fields, parentId.Name, new InferredType($"{{ {member.MemberName}: any }}", false));
                }

                break;
            case CallNode call:
                CollectFromExpression(call.Function, fields);
                foreach (var arg in call.Arguments)
                {
                    CollectFromExpression(arg, fields);
                }

                break;
            case IndexNode index:
                CollectFromExpression(index.Object, fields);
                CollectFromExpression(index.Index, fields);
                break;
            case PipeNode pipe:
                CollectFromExpression(pipe.Left, fields);
                foreach (var arg in pipe.Arguments)
                {
                    CollectFromExpression(arg, fields);
                }

                break;
        }
    }

    private static void MergeField(Dictionary<string, InferredType> fields, string name, InferredType type)
    {
        if (!fields.TryGetValue(name, out var existing))
        {
            fields[name] = type;
            return;
        }

        if (type.IsArray && !existing.IsArray)
        {
            fields[name] = type;
        }
    }

    private static string GenerateInterface(Dictionary<string, InferredType> fields, string interfaceName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"export interface {interfaceName} {{");

        foreach (var (name, type) in fields.OrderBy(f => f.Key))
        {
            sb.AppendLine($"    {name}: {type.TypeName};");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private sealed record InferredType(string TypeName, bool IsArray);
}
