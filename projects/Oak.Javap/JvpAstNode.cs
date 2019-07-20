using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Javap;

/// <summary>
///     Javap AST 基类
/// </summary>
/// <param name="Span">源码位置</param>
public abstract record JvpAstNode(TextSpan Span = default(TextSpan));

/// <summary>
///     javap -c 反汇编输出
/// </summary>
/// <param name="SourceFile">源文件名</param>
/// <param name="ClassDecl">类声明</param>
/// <param name="Span">源码位置</param>
public sealed record JvpDisassembly(string? SourceFile, JvpClassDeclaration ClassDecl, TextSpan Span = default(TextSpan)) : JvpAstNode(Span);

/// <summary>
///     类声明
/// </summary>
/// <param name="AccessFlags">访问标志</param>
/// <param name="ClassKind">类种类（class/interface/enum）</param>
/// <param name="ClassName">类名（全限定名）</param>
/// <param name="Extends">父类</param>
/// <param name="Implements">实现的接口</param>
/// <param name="Fields">字段列表</param>
/// <param name="Methods">方法列表</param>
/// <param name="Span">源码位置</param>
public sealed record JvpClassDeclaration(
    List<string> AccessFlags,
    string ClassKind,
    string ClassName,
    string? Extends,
    List<string> Implements,
    List<JvpFieldDeclaration> Fields,
    List<JvpMethodDeclaration> Methods,
    TextSpan Span = default(TextSpan)) : JvpAstNode(Span);

/// <summary>
///     字段声明
/// </summary>
/// <param name="AccessFlags">访问标志</param>
/// <param name="TypeName">类型名</param>
/// <param name="FieldName">字段名</param>
/// <param name="Span">源码位置</param>
public sealed record JvpFieldDeclaration(List<string> AccessFlags, string TypeName, string FieldName, TextSpan Span = default(TextSpan)) : JvpAstNode(Span);

/// <summary>
///     方法声明
/// </summary>
/// <param name="AccessFlags">访问标志</param>
/// <param name="ReturnTypeName">返回类型名</param>
/// <param name="MethodName">方法名</param>
/// <param name="Parameters">参数列表</param>
/// <param name="CodeSection">Code 段</param>
/// <param name="Span">源码位置</param>
public sealed record JvpMethodDeclaration(
    List<string> AccessFlags,
    string ReturnTypeName,
    string MethodName,
    List<JvpParameter> Parameters,
    JvpCodeSection? CodeSection,
    TextSpan Span = default(TextSpan)) : JvpAstNode(Span);

/// <summary>
///     方法参数
/// </summary>
/// <param name="TypeName">类型名</param>
/// <param name="Name">参数名</param>
/// <param name="Span">源码位置</param>
public sealed record JvpParameter(string TypeName, string? Name = null, TextSpan Span = default(TextSpan)) : JvpAstNode(Span);

/// <summary>
///     Code 段
/// </summary>
/// <param name="Instructions">指令列表</param>
/// <param name="Span">源码位置</param>
public sealed record JvpCodeSection(List<JvpInstruction> Instructions, TextSpan Span = default(TextSpan)) : JvpAstNode(Span);

/// <summary>
///     JVM 指令（javap 格式）
/// </summary>
/// <param name="Offset">字节码偏移量</param>
/// <param name="Opcode">操作码名称（小写下划线格式）</param>
/// <param name="Operand">操作数文本</param>
/// <param name="Comment">常量池注释</param>
/// <param name="Span">源码位置</param>
public sealed record JvpInstruction(int Offset, string Opcode, string? Operand = null, string? Comment = null, TextSpan Span = default(TextSpan)) : JvpAstNode(Span);
