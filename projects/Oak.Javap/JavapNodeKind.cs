using Oak.Syntax;

namespace Oak.Javap;

/// <summary>
///     Javap 语法节点类型
/// </summary>
public static class JavapNodeKind
{
    public static readonly NodeKind Unknown = 0;

    #region 顶层结构

    public static readonly NodeKind CompiledFromHeader = 1;
    public static readonly NodeKind ClassDeclaration = 2;
    public static readonly NodeKind FieldDeclaration = 3;
    public static readonly NodeKind MethodDeclaration = 4;

    #endregion

    #region 方法体

    public static readonly NodeKind CodeSection = 10;
    public static readonly NodeKind Instruction = 11;
    public static readonly NodeKind ExceptionTable = 12;
    public static readonly NodeKind LineNumberTable = 13;
    public static readonly NodeKind StackMapTable = 14;

    #endregion

    #region Token 类型

    public static readonly NodeKind AccessModifier = 20;
    public static readonly NodeKind TypeKeyword = 21;
    public static readonly NodeKind Opcode = 22;
    public static readonly NodeKind Identifier = 23;
    public static readonly NodeKind Number = 24;
    public static readonly NodeKind ConstantPoolRef = 25;
    public static readonly NodeKind Comment = 26;
    public static readonly NodeKind Punctuation = 27;
    public static readonly NodeKind Eof = 28;

    #endregion
}
