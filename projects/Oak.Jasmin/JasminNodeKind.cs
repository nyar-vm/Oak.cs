using Oak.Syntax;

namespace Oak.Jasmin;

/// <summary>
///     Jasmin 语法节点类型
/// </summary>
public static class JasminNodeKind
{
    public static readonly NodeKind Unknown = 0;

    #region 顶层声明

    public static readonly NodeKind ClassDirective = 1;
    public static readonly NodeKind SuperDirective = 2;
    public static readonly NodeKind ImplementsDirective = 3;
    public static readonly NodeKind InterfaceDirective = 4;
    public static readonly NodeKind FieldDirective = 5;
    public static readonly NodeKind MethodDirective = 6;
    public static readonly NodeKind EndMethodDirective = 7;
    public static readonly NodeKind SourceDirective = 8;
    public static readonly NodeKind VersionDirective = 9;

    #endregion

    #region 方法体指令

    public static readonly NodeKind LimitDirective = 10;
    public static readonly NodeKind LineDirective = 11;
    public static readonly NodeKind VarDirective = 12;
    public static readonly NodeKind ThrowsDirective = 13;
    public static readonly NodeKind CatchDirective = 14;
    public static readonly NodeKind Instruction = 15;
    public static readonly NodeKind Label = 16;

    #endregion

    #region Token 类型

    public static readonly NodeKind Directive = 20;
    public static readonly NodeKind Opcode = 21;
    public static readonly NodeKind Identifier = 22;
    public static readonly NodeKind Descriptor = 23;
    public static readonly NodeKind Number = 24;
    public static readonly NodeKind StringLiteral = 25;
    public static readonly NodeKind Comment = 26;
    public static readonly NodeKind Eof = 27;

    #endregion
}
