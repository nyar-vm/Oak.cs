using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Jasmin;

/// <summary>
///     Jasmin AST 基类
/// </summary>
/// <param name="Span">源码位置</param>
public abstract record JmAstNode(TextSpan Span = default(TextSpan));

/// <summary>
///     Jasmin 类文件
/// </summary>
/// <param name="ClassDirective">.class 指令</param>
/// <param name="SuperDirective">.super 指令</param>
/// <param name="ImplementsDirectives">.implements 指令列表</param>
/// <param name="Fields">字段列表</param>
/// <param name="Methods">方法列表</param>
/// <param name="Span">源码位置</param>
public sealed record JmClassFile(
    JmClassDirective ClassDirective,
    JmSuperDirective SuperDirective,
    List<JmImplementsDirective> ImplementsDirectives,
    List<JmFieldDirective> Fields,
    List<JmMethodDeclaration> Methods,
    TextSpan Span = default(TextSpan)) : JmAstNode(Span);

/// <summary>
///     .class 指令
/// </summary>
/// <param name="AccessFlags">访问标志</param>
/// <param name="ClassName">类名（内部格式）</param>
/// <param name="Span">源码位置</param>
public sealed record JmClassDirective(List<string> AccessFlags, string ClassName, TextSpan Span = default(TextSpan)) : JmAstNode(Span);

/// <summary>
///     .super 指令
/// </summary>
/// <param name="SuperClassName">父类名（内部格式）</param>
/// <param name="Span">源码位置</param>
public sealed record JmSuperDirective(string SuperClassName, TextSpan Span = default(TextSpan)) : JmAstNode(Span);

/// <summary>
///     .implements 指令
/// </summary>
/// <param name="InterfaceName">接口名（内部格式）</param>
/// <param name="Span">源码位置</param>
public sealed record JmImplementsDirective(string InterfaceName, TextSpan Span = default(TextSpan)) : JmAstNode(Span);

/// <summary>
///     .field 指令
/// </summary>
/// <param name="AccessFlags">访问标志</param>
/// <param name="FieldName">字段名</param>
/// <param name="Descriptor">字段描述符</param>
/// <param name="InitialValue">初始值</param>
/// <param name="Span">源码位置</param>
public sealed record JmFieldDirective(List<string> AccessFlags, string FieldName, string Descriptor, string? InitialValue = null, TextSpan Span = default(TextSpan)) : JmAstNode(Span);

/// <summary>
///     .method / .end method 声明
/// </summary>
/// <param name="AccessFlags">访问标志</param>
/// <param name="MethodName">方法名</param>
/// <param name="Descriptor">方法描述符</param>
/// <param name="Limits">.limit 指令列表</param>
/// <param name="Instructions">指令列表</param>
/// <param name="Span">源码位置</param>
public sealed record JmMethodDeclaration(
    List<string> AccessFlags,
    string MethodName,
    string Descriptor,
    List<JmLimitDirective> Limits,
    List<JmInstruction> Instructions,
    TextSpan Span = default(TextSpan)) : JmAstNode(Span);

/// <summary>
///     .limit 指令
/// </summary>
/// <param name="LimitKind">限制类型（stack / locals）</param>
/// <param name="Value">限制值</param>
/// <param name="Span">源码位置</param>
public sealed record JmLimitDirective(string LimitKind, int Value, TextSpan Span = default(TextSpan)) : JmAstNode(Span);

/// <summary>
///     JVM 指令
/// </summary>
/// <param name="Opcode">操作码名称</param>
/// <param name="Operands">操作数列表</param>
/// <param name="Label">标签（如果指令前有标签）</param>
/// <param name="Span">源码位置</param>
public sealed record JmInstruction(string Opcode, List<string> Operands, string? Label = null, TextSpan Span = default(TextSpan)) : JmAstNode(Span);
