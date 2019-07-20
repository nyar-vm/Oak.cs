using Oak.Syntax;

namespace Oak.Msil;

/// <summary>
///     MSIL 语法节点类型
/// </summary>
public static class MsilNodeKind
{
    public static readonly NodeKind Unknown = 0;

    #region 顶层声明

    public static readonly NodeKind AssemblyDirective = 1;
    public static readonly NodeKind ModuleDirective = 2;
    public static readonly NodeKind NamespaceDirective = 3;
    public static readonly NodeKind ClassDirective = 4;
    public static readonly NodeKind MethodDirective = 5;
    public static readonly NodeKind FieldDirective = 6;

    #endregion

    #region 方法体

    public static readonly NodeKind MaxStackDirective = 10;
    public static readonly NodeKind LocalsDirective = 11;
    public static readonly NodeKind EntryPointDirective = 12;
    public static readonly NodeKind Instruction = 13;
    public static readonly NodeKind Label = 14;

    #endregion

    #region Token 类型

    public static readonly NodeKind Directive = 20;
    public static readonly NodeKind Opcode = 21;
    public static readonly NodeKind Identifier = 22;
    public static readonly NodeKind TypeReference = 23;
    public static readonly NodeKind Number = 24;
    public static readonly NodeKind StringLiteral = 25;
    public static readonly NodeKind Comment = 26;
    public static readonly NodeKind Eof = 27;

    #endregion
}
