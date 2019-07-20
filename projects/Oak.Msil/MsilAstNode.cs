using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Msil;

/// <summary>
///     ILASM AST 基类
/// </summary>
/// <param name="Span">源码位置</param>
public abstract record MsilAstNode(TextSpan Span = default(TextSpan));

/// <summary>
///     ILASM 程序集
/// </summary>
/// <param name="Name">程序集名</param>
/// <param name="Version">版本号</param>
/// <param name="Namespaces">命名空间列表</param>
/// <param name="Span">源码位置</param>
public sealed record MsilAssembly(string Name, string? Version, List<MsilNamespace> Namespaces, TextSpan Span = default(TextSpan)) : MsilAstNode(Span);

/// <summary>
///     命名空间
/// </summary>
/// <param name="Name">命名空间名</param>
/// <param name="Types">类型列表</param>
/// <param name="Span">源码位置</param>
public sealed record MsilNamespace(string Name, List<MsilClassDeclaration> Types, TextSpan Span = default(TextSpan)) : MsilAstNode(Span);

/// <summary>
///     类声明
/// </summary>
/// <param name="AccessFlags">访问标志</param>
/// <param name="TypeKind">类型种类（class / value type）</param>
/// <param name="Name">类型名</param>
/// <param name="Methods">方法列表</param>
/// <param name="Fields">字段列表</param>
/// <param name="Span">源码位置</param>
public sealed record MsilClassDeclaration(
    List<string> AccessFlags,
    string TypeKind,
    string Name,
    List<MsilMethodDeclaration> Methods,
    List<MsilFieldDeclaration> Fields,
    TextSpan Span = default(TextSpan)) : MsilAstNode(Span);

/// <summary>
///     字段声明
/// </summary>
/// <param name="AccessFlags">访问标志</param>
/// <param name="TypeName">类型名</param>
/// <param name="FieldName">字段名</param>
/// <param name="Span">源码位置</param>
public sealed record MsilFieldDeclaration(List<string> AccessFlags, string TypeName, string FieldName, TextSpan Span = default(TextSpan)) : MsilAstNode(Span);

/// <summary>
///     方法声明
/// </summary>
/// <param name="AccessFlags">访问标志</param>
/// <param name="ReturnTypeName">返回类型名</param>
/// <param name="MethodName">方法名</param>
/// <param name="Parameters">参数列表</param>
/// <param name="MaxStack">最大栈深度</param>
/// <param name="LocalVariables">局部变量列表</param>
/// <param name="Instructions">指令列表</param>
/// <param name="IsEntryPoint">是否为入口点</param>
/// <param name="Span">源码位置</param>
public sealed record MsilMethodDeclaration(
    List<string> AccessFlags,
    string ReturnTypeName,
    string MethodName,
    List<MsilParameter> Parameters,
    int MaxStack,
    List<MsilLocalVariable> LocalVariables,
    List<MsilInstruction> Instructions,
    bool IsEntryPoint = false,
    TextSpan Span = default(TextSpan)) : MsilAstNode(Span);

/// <summary>
///     方法参数
/// </summary>
/// <param name="TypeName">类型名</param>
/// <param name="Name">参数名</param>
/// <param name="Span">源码位置</param>
public sealed record MsilParameter(string TypeName, string Name, TextSpan Span = default(TextSpan)) : MsilAstNode(Span);

/// <summary>
///     局部变量
/// </summary>
/// <param name="TypeName">类型名</param>
/// <param name="Name">变量名</param>
/// <param name="Index">索引</param>
/// <param name="Span">源码位置</param>
public sealed record MsilLocalVariable(string TypeName, string? Name = null, int Index = 0, TextSpan Span = default(TextSpan)) : MsilAstNode(Span);

/// <summary>
///     MSIL 指令
/// </summary>
/// <param name="Offset">字节码偏移量</param>
/// <param name="Opcode">操作码名称（点分格式：ldarg.0, call, add 等）</param>
/// <param name="Operand">操作数文本</param>
/// <param name="Span">源码位置</param>
public sealed record MsilInstruction(int Offset, string Opcode, string? Operand = null, TextSpan Span = default(TextSpan)) : MsilAstNode(Span);
