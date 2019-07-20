using System.Collections.Generic;
using Oak.DejaVu.Expressions;

namespace Oak.DejaVu;

/// <summary>
///     DejaVu 模板节点基类
/// </summary>
public abstract class DejaVuTemplateNode
{
    /// <summary>
    ///     节点类型
    /// </summary>
    public abstract DejaVuNodeType NodeType { get; }

    /// <summary>
    ///     源码行号（1-based，0 表示未知）
    /// </summary>
    public int SourceLine { get; init; }

    /// <summary>
    ///     源码列号（1-based，0 表示未知）
    /// </summary>
    public int SourceColumn { get; init; }
}

/// <summary>
///     DejaVu 节点类型
/// </summary>
public enum DejaVuNodeType
{
    /// <summary>
    ///     文本节点
    /// </summary>
    Text,

    /// <summary>
    ///     代码节点
    /// </summary>
    Code,

    /// <summary>
    ///     if 节点
    /// </summary>
    If,

    /// <summary>
    ///     loop 节点
    /// </summary>
    Loop,

    /// <summary>
    ///     match 节点
    /// </summary>
    Match,

    /// <summary>
    ///     block 节点
    /// </summary>
    Block,

    /// <summary>
    ///     extends 节点
    /// </summary>
    Extends,

    /// <summary>
    ///     include 节点
    /// </summary>
    Include,

    /// <summary>
    ///     let 节点
    /// </summary>
    Let,

    /// <summary>
    ///     with 节点
    /// </summary>
    With,

    /// <summary>
    ///     super 节点
    /// </summary>
    Super,

    /// <summary>
    ///     raw 节点（原始输出）
    /// </summary>
    Raw
}

#region 节点类

/// <summary>
///     文本节点
/// </summary>
public sealed class DejaVuTextNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Text;

    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text { get; init; } = string.Empty;
}

/// <summary>
///     代码节点
/// </summary>
public sealed class DejaVuCodeNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Code;

    /// <summary>
    ///     代码内容
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    ///     预解析的表达式 AST
    /// </summary>
    public IExpressionNode? ParsedExpression { get; init; }
}

/// <summary>
///     if 节点
/// </summary>
public sealed class DejaVuIfNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.If;

    /// <summary>
    ///     条件表达式原始文本
    /// </summary>
    public string Condition { get; init; } = string.Empty;

    /// <summary>
    ///     预解析的条件表达式 AST
    /// </summary>
    public IExpressionNode? ParsedCondition { get; init; }

    /// <summary>
    ///     子节点
    /// </summary>
    public List<DejaVuTemplateNode> Children { get; init; } = [];

    /// <summary>
    ///     else 子节点
    /// </summary>
    public List<DejaVuTemplateNode> ElseChildren { get; init; } = [];

    /// <summary>
    ///     else if 节点列表
    /// </summary>
    public List<DejaVuElseIfNode> ElseIfNodes { get; init; } = [];
}

/// <summary>
///     else if 节点
/// </summary>
public sealed class DejaVuElseIfNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.If;

    /// <summary>
    ///     条件表达式原始文本
    /// </summary>
    public string Condition { get; init; } = string.Empty;

    /// <summary>
    ///     预解析的条件表达式 AST
    /// </summary>
    public IExpressionNode? ParsedCondition { get; init; }

    /// <summary>
    ///     子节点
    /// </summary>
    public List<DejaVuTemplateNode> Children { get; init; } = [];
}

#endregion

/// <summary>
///     loop 节点
/// </summary>
public sealed class DejaVuLoopNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Loop;

    /// <summary>
    ///     循环表达式（如 "items" 或 "items" 当用 loop in 语法时）
    /// </summary>
    public string Expression { get; init; } = string.Empty;

    /// <summary>
    ///     预解析的表达式 AST
    /// </summary>
    public IExpressionNode? ParsedExpression { get; init; }

    /// <summary>
    ///     迭代变量名（loop in 语法时使用，如 "item"）
    /// </summary>
    public string? ItemName { get; init; }

    /// <summary>
    ///     子节点
    /// </summary>
    public List<DejaVuTemplateNode> Children { get; init; } = [];
}

/// <summary>
///     match 节点
/// </summary>
public sealed class DejaVuMatchNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Match;

    /// <summary>
    ///     match 表达式
    /// </summary>
    public string Expression { get; init; } = string.Empty;

    /// <summary>
    ///     预解析的表达式 AST
    /// </summary>
    public IExpressionNode? ParsedExpression { get; init; }

    /// <summary>
    ///     子节点
    /// </summary>
    public List<DejaVuTemplateNode> Children { get; init; } = [];
}

/// <summary>
///     block 节点
/// </summary>
public sealed class DejaVuBlockNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Block;

    /// <summary>
    ///     block 名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     子节点
    /// </summary>
    public List<DejaVuTemplateNode> Children { get; init; } = [];
}

/// <summary>
///     extends 节点
/// </summary>
public sealed class DejaVuExtendsNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Extends;

    /// <summary>
    ///     父模板路径
    /// </summary>
    public string ParentTemplate { get; init; } = string.Empty;
}

/// <summary>
///     include 节点
/// </summary>
public sealed class DejaVuIncludeNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Include;

    /// <summary>
    ///     包含的模板路径
    /// </summary>
    public string TemplatePath { get; init; } = string.Empty;
}

/// <summary>
///     let 节点（局部变量绑定）
/// </summary>
public sealed class DejaVuLetNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Let;

    /// <summary>
    ///     变量名
    /// </summary>
    public string VariableName { get; init; } = string.Empty;

    /// <summary>
    ///     值表达式
    /// </summary>
    public string Expression { get; init; } = string.Empty;

    /// <summary>
    ///     预解析的表达式 AST
    /// </summary>
    public IExpressionNode? ParsedExpression { get; init; }

    /// <summary>
    ///     子节点
    /// </summary>
    public List<DejaVuTemplateNode> Children { get; init; } = [];
}

/// <summary>
///     with 节点（作用域别名）
/// </summary>
public sealed class DejaVuWithNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.With;

    /// <summary>
    ///     别名
    /// </summary>
    public string AliasName { get; init; } = string.Empty;

    /// <summary>
    ///     表达式
    /// </summary>
    public string Expression { get; init; } = string.Empty;

    /// <summary>
    ///     预解析的表达式 AST
    /// </summary>
    public IExpressionNode? ParsedExpression { get; init; }

    /// <summary>
    ///     子节点
    /// </summary>
    public List<DejaVuTemplateNode> Children { get; init; } = [];
}

/// <summary>
///     super 节点（渲染父模板的 block 默认内容）
/// </summary>
public sealed class DejaVuSuperNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Super;
}

/// <summary>
///     raw 节点（原始 HTML 输出，不转义）
/// </summary>
public sealed class DejaVuRawNode : DejaVuTemplateNode
{
    /// <inheritdoc />
    public override DejaVuNodeType NodeType => DejaVuNodeType.Raw;

    /// <summary>
    ///     子节点
    /// </summary>
    public List<DejaVuTemplateNode> Children { get; init; } = [];
}

