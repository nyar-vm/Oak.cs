using Oak.Syntax;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Pattern;
using Oak.Valkyrie.AST.Statement;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST;

/// <summary>
///     AST 节点基类，所有 AST 节点的抽象根类型
/// </summary>
/// <para>继承体系：</para>
/// <list type="bullet">
///     <item>声明类：<see cref="DeclareMicro"/>、<see cref="DeclareClass"/>、<see cref="DeclareStructure"/>、<see cref="DeclareEnums"/> 等</item>
///     <item>语句类：<see cref="IfStatement"/>、<see cref="LoopStatement"/>、<see cref="WhileStatement"/>、<see cref="ReturnStatement"/> 等</item>
///     <item>表达式类：<see cref="TermAtomicLiteral"/>、<see cref="BinaryExpr"/>、<see cref="AnonymousMicro"/> 等</item>
///     <item>模式类：<see cref="ConstantPattern"/>、<see cref="DeclarationPattern"/>、<see cref="PatternNode"/>、<see cref="WildcardPattern"/></item>
/// </list>
public abstract record ValkyrieNode
{
    /// <summary>
    ///     节点类型标识，默认通过类型名称自动映射到 <see cref="ValkyrieNodeType"/> 枚举
    /// </summary>
    public virtual ValkyrieNodeType Type => Enum.TryParse<ValkyrieNodeType>(GetType().Name, out var result) ? result : ValkyrieNodeType.Unknown;

    /// <summary>
    ///     源代码位置范围（偏移量 + 长度），用于错误报告和 IDE 定位
    /// </summary>
    public TextSpan Span { get; init; }
}