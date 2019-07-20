using Oak.DejaVu.Expressions;
using Oak.Diagnostics;

namespace Oak.DejaVu.Optimizer;

/// <summary>
///     编译期符号解析器——变量作用域分析、引用验证、继承链检查。
/// </summary>
public sealed class SymbolResolver
{
    private readonly DiagnosticSink _diagnostics;

    /// <summary>
    ///     创建符号解析器
    /// </summary>
    /// <param name="diagnostics">诊断消息收集器</param>
    public SymbolResolver(DiagnosticSink diagnostics)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     对编译后的模板节点执行符号解析
    /// </summary>
    /// <returns>解析出的符号表</returns>
    public SymbolTable Resolve(IReadOnlyList<DejaVuTemplateNode> nodes)
    {
        var globalScope = new Scope(null, "<global>");
        var symbolTable = new SymbolTable(globalScope);

        ResolveNodes(nodes, globalScope, symbolTable);

        ValidateScopes(symbolTable);

        return symbolTable;
    }

    private void ResolveNodes(IReadOnlyList<DejaVuTemplateNode> nodes, Scope parentScope, SymbolTable symbolTable)
    {
        foreach (var node in nodes)
        {
            ResolveNode(node, parentScope, symbolTable);
        }
    }

    private void ResolveNode(DejaVuTemplateNode node, Scope parentScope, SymbolTable symbolTable)
    {
        switch (node)
        {
            case DejaVuIfNode ifNode:
                ResolveIfNode(ifNode, parentScope, symbolTable);
                break;
            case DejaVuLoopNode loopNode:
                ResolveLoopNode(loopNode, parentScope, symbolTable);
                break;
            case DejaVuLetNode letNode:
                ResolveLetNode(letNode, parentScope, symbolTable);
                break;
            case DejaVuWithNode withNode:
                ResolveWithNode(withNode, parentScope, symbolTable);
                break;
            case DejaVuBlockNode blockNode:
                ResolveBlockNode(blockNode, parentScope, symbolTable);
                break;
            case DejaVuCodeNode codeNode:
                ResolveExpressionReferences(codeNode.ParsedExpression, parentScope);
                break;
            case DejaVuExtendsNode extendsNode:
                ResolveExtendsNode(extendsNode, symbolTable);
                break;
            case DejaVuIncludeNode includeNode:
                ResolveIncludeNode(includeNode, symbolTable);
                break;
            case DejaVuMatchNode matchNode:
                ResolveMatchNode(matchNode, parentScope, symbolTable);
                break;
            case DejaVuRawNode rawNode:
                ResolveNodes(rawNode.Children, parentScope, symbolTable);
                break;
        }
    }

    private void ResolveIfNode(DejaVuIfNode ifNode, Scope parentScope, SymbolTable symbolTable)
    {
        ResolveExpressionReferences(ifNode.ParsedCondition, parentScope);
        ResolveNodes(ifNode.Children, parentScope, symbolTable);

        foreach (var elseIfNode in ifNode.ElseIfNodes)
        {
            ResolveExpressionReferences(elseIfNode.ParsedCondition, parentScope);
            ResolveNodes(elseIfNode.Children, parentScope, symbolTable);
        }

        ResolveNodes(ifNode.ElseChildren, parentScope, symbolTable);
    }

    private void ResolveLoopNode(DejaVuLoopNode loopNode, Scope parentScope, SymbolTable symbolTable)
    {
        ResolveExpressionReferences(loopNode.ParsedExpression, parentScope);

        var loopScope = new Scope(parentScope, "loop");
        var itemName = loopNode.ItemName ?? "item";
        loopScope.Declare(itemName, SymbolKind.IterationVariable);
        loopScope.Declare("index", SymbolKind.IterationVariable);

        symbolTable.AddScope(loopScope);
        ResolveNodes(loopNode.Children, loopScope, symbolTable);
    }

    private void ResolveLetNode(DejaVuLetNode letNode, Scope parentScope, SymbolTable symbolTable)
    {
        ResolveExpressionReferences(letNode.ParsedExpression, parentScope);

        var letScope = new Scope(parentScope, $"let:{letNode.VariableName}");
        letScope.Declare(letNode.VariableName, SymbolKind.LocalVariable);

        symbolTable.AddScope(letScope);
        ResolveNodes(letNode.Children, letScope, symbolTable);
    }

    private void ResolveWithNode(DejaVuWithNode withNode, Scope parentScope, SymbolTable symbolTable)
    {
        ResolveExpressionReferences(withNode.ParsedExpression, parentScope);

        var withScope = new Scope(parentScope, $"with:{withNode.AliasName}");

        // with 块内的 .member 访问现在从别名对象解析，声明别名
        if (!string.IsNullOrEmpty(withNode.AliasName))
        {
            withScope.Declare(withNode.AliasName, SymbolKind.ScopeAlias);
        }

        symbolTable.AddScope(withScope);
        ResolveNodes(withNode.Children, withScope, symbolTable);
    }

    private void ResolveBlockNode(DejaVuBlockNode blockNode, Scope parentScope, SymbolTable symbolTable)
    {
        var blockScope = new Scope(parentScope, $"block:{blockNode.Name}");
        symbolTable.AddScope(blockScope);
        symbolTable.RegisterBlock(blockNode.Name);
        ResolveNodes(blockNode.Children, blockScope, symbolTable);
    }

    private void ResolveExtendsNode(DejaVuExtendsNode extendsNode, SymbolTable symbolTable)
    {
        var parentTemplate = extendsNode.ParentTemplate.Trim('\'', '"');
        symbolTable.ParentTemplate = parentTemplate;
    }

    private void ResolveIncludeNode(DejaVuIncludeNode includeNode, SymbolTable symbolTable)
    {
        var templatePath = includeNode.TemplatePath.Trim('\'', '"');
        symbolTable.IncludedTemplates.Add(templatePath);
    }

    private void ResolveMatchNode(DejaVuMatchNode matchNode, Scope parentScope, SymbolTable symbolTable)
    {
        ResolveExpressionReferences(matchNode.ParsedExpression, parentScope);
        ResolveNodes(matchNode.Children, parentScope, symbolTable);
    }

    /// <summary>
    ///     从表达式 AST 中收集标识符引用
    /// </summary>
    private void ResolveExpressionReferences(IExpressionNode? node, Scope scope)
    {
        if (node == null)
        {
            return;
        }

        switch (node)
        {
            case IdentifierNode identifier:
                scope.Reference(identifier.Name);
                break;
            case BinaryNode binary:
                ResolveExpressionReferences(binary.Left, scope);
                ResolveExpressionReferences(binary.Right, scope);
                break;
            case UnaryNode unary:
                ResolveExpressionReferences(unary.Operand, scope);
                break;
            case MemberAccessNode memberAccess:
                ResolveExpressionReferences(memberAccess.Object, scope);
                break;
            case CallNode call:
                ResolveExpressionReferences(call.Function, scope);
                foreach (var arg in call.Arguments)
                {
                    ResolveExpressionReferences(arg, scope);
                }

                break;
            case IndexNode index:
                ResolveExpressionReferences(index.Object, scope);
                ResolveExpressionReferences(index.Index, scope);
                break;
            case PipeNode pipe:
                ResolveExpressionReferences(pipe.Left, scope);
                foreach (var arg in pipe.Arguments)
                {
                    ResolveExpressionReferences(arg, scope);
                }

                break;
        }
    }

    /// <summary>
    ///     验证所有作用域中的引用是否合法
    /// </summary>
    private void ValidateScopes(SymbolTable symbolTable)
    {
        foreach (var scope in symbolTable.AllScopes)
        {
            foreach (var (name, isDeclared) in scope.References)
            {
                if (isDeclared)
                {
                    continue;
                }

                // 检查是否是全局上下文变量（运行期绑定）
                if (symbolTable.GlobalScope.IsDeclared(name))
                {
                    scope.MarkDeclared(name);
                    continue;
                }

                // 未声明的变量——在编译期报告警告（运行期可能从上下文中绑定）
                _diagnostics.AddWarning(
                    string.Empty,
                    default,
                    "UndefinedVariable",
                    $"未声明的变量 \"{name}\" 在作用域 \"{scope.Name}\" 中被引用，运行期将从模板上下文中解析"
                );
            }
        }
    }
}

/// <summary>
///     符号表——编译期收集的所有符号信息
/// </summary>
public sealed class SymbolTable
{
    private readonly List<Scope> _scopes = [];

    /// <summary>
    ///     全局作用域
    /// </summary>
    public Scope GlobalScope { get; }

    /// <summary>
    ///     所有作用域
    /// </summary>
    public IReadOnlyList<Scope> AllScopes => _scopes;

    /// <summary>
    ///     父模板路径（extends 引用）
    /// </summary>
    public string? ParentTemplate { get; set; }

    /// <summary>
    ///     所有引入的模板路径
    /// </summary>
    public List<string> IncludedTemplates { get; } = [];

    /// <summary>
    ///     所有 block 名称
    /// </summary>
    public HashSet<string> Blocks { get; } = [];

    /// <summary>
    ///     创建符号表
    /// </summary>
    public SymbolTable(Scope globalScope)
    {
        GlobalScope = globalScope;
        _scopes.Add(globalScope);
    }

    /// <summary>
    ///     添加作用域
    /// </summary>
    public void AddScope(Scope scope)
    {
        _scopes.Add(scope);
    }

    /// <summary>
    ///     注册 block 名称
    /// </summary>
    public void RegisterBlock(string name)
    {
        Blocks.Add(name);
    }
}

/// <summary>
///     作用域——变量声明的命名空间
/// </summary>
public sealed class Scope
{
    private readonly Dictionary<string, SymbolKind> _declarations = new();
    private readonly Dictionary<string, bool> _references = new();

    /// <summary>
    ///     作用域名称（用于调试）
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     父作用域
    /// </summary>
    public Scope? Parent { get; }

    /// <summary>
    ///     变量引用及其声明状态（true = 已声明）
    /// </summary>
    public IReadOnlyDictionary<string, bool> References => _references;

    /// <summary>
    ///     创建作用域
    /// </summary>
    public Scope(Scope? parent, string name)
    {
        Parent = parent;
        Name = name;
    }

    /// <summary>
    ///     声明变量
    /// </summary>
    public void Declare(string name, SymbolKind kind)
    {
        _declarations[name] = kind;
    }

    /// <summary>
    ///     记录变量引用（在当前作用域或父作用域中查找声明）
    /// </summary>
    public void Reference(string name)
    {
        if (_references.ContainsKey(name))
        {
            return;
        }

        var isDeclared = IsDeclaredInChain(name);
        _references[name] = isDeclared;
    }

    /// <summary>
    ///     检查变量是否在当前作用域中声明
    /// </summary>
    public bool IsDeclared(string name)
    {
        return _declarations.ContainsKey(name);
    }

    /// <summary>
    ///     标记变量为已声明
    /// </summary>
    public void MarkDeclared(string name)
    {
        if (_references.ContainsKey(name))
        {
            _references[name] = true;
        }
    }

    private bool IsDeclaredInChain(string name)
    {
        var current = this;
        while (current != null)
        {
            if (current._declarations.ContainsKey(name))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }
}

/// <summary>
///     符号类型
/// </summary>
public enum SymbolKind
{
    /// <summary>
    ///     迭代变量（loop item）
    /// </summary>
    IterationVariable,

    /// <summary>
    ///     局部变量（let 绑定）
    /// </summary>
    LocalVariable,

    /// <summary>
    ///     作用域别名（with 绑定）
    /// </summary>
    ScopeAlias
}
