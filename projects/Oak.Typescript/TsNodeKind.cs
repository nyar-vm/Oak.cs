using Oak.Syntax;

namespace Oak.Typescript;

/// <summary>
///     TypeScript 词法节点类型
/// </summary>
public static class TsNodeKind
{
    #region 词法节点

    public static readonly NodeKind Eof = 0;
    public static readonly NodeKind Identifier = 1;
    public static readonly NodeKind Keyword = 2;
    public static readonly NodeKind Number = 3;
    public static readonly NodeKind String = 4;
    public static readonly NodeKind TemplateString = 5;
    public static readonly NodeKind Operator = 6;
    public static readonly NodeKind Delimiter = 7;
    public static readonly NodeKind Punctuation = 8;
    public static readonly NodeKind Literal = 9;
    public static readonly NodeKind Attribute = 10;
    public static readonly NodeKind Comment = 11;
    public static readonly NodeKind JsxText = 12;

    #endregion

    #region 语句节点

    /// <summary>
    ///     块语句
    /// </summary>
    public static readonly NodeKind BlockStmt = 100;

    /// <summary>
    ///     if 条件语句
    /// </summary>
    public static readonly NodeKind IfStmt = 101;

    /// <summary>
    ///     for 循环语句
    /// </summary>
    public static readonly NodeKind ForStmt = 102;

    /// <summary>
    ///     while 循环语句
    /// </summary>
    public static readonly NodeKind WhileStmt = 103;

    /// <summary>
    ///     return 语句
    /// </summary>
    public static readonly NodeKind ReturnStmt = 104;

    /// <summary>
    ///     表达式语句
    /// </summary>
    public static readonly NodeKind ExprStmt = 105;

    /// <summary>
    ///     do-while 循环语句
    /// </summary>
    public static readonly NodeKind DoWhileStmt = 106;

    /// <summary>
    ///     switch 语句
    /// </summary>
    public static readonly NodeKind SwitchStmt = 107;

    /// <summary>
    ///     switch case 子句
    /// </summary>
    public static readonly NodeKind SwitchCase = 108;

    /// <summary>
    ///     try-catch-finally 语句
    /// </summary>
    public static readonly NodeKind TryStmt = 109;

    /// <summary>
    ///     catch 子句
    /// </summary>
    public static readonly NodeKind CatchClause = 110;

    /// <summary>
    ///     throw 语句
    /// </summary>
    public static readonly NodeKind ThrowStmt = 111;

    /// <summary>
    ///     break 语句
    /// </summary>
    public static readonly NodeKind BreakStmt = 112;

    /// <summary>
    ///     continue 语句
    /// </summary>
    public static readonly NodeKind ContinueStmt = 113;

    /// <summary>
    ///     for-in 语句
    /// </summary>
    public static readonly NodeKind ForInStmt = 114;

    /// <summary>
    ///     for-of 语句
    /// </summary>
    public static readonly NodeKind ForOfStmt = 115;

    /// <summary>
    ///     debugger 语句
    /// </summary>
    public static readonly NodeKind DebuggerStmt = 116;

    /// <summary>
    ///     空语句
    /// </summary>
    public static readonly NodeKind EmptyStmt = 117;

    /// <summary>
    ///     标签语句
    /// </summary>
    public static readonly NodeKind LabeledStmt = 118;

    #endregion

    #region 表达式节点

    /// <summary>
    ///     标识符表达式
    /// </summary>
    public static readonly NodeKind IdentifierExpr = 200;

    /// <summary>
    ///     字面量表达式
    /// </summary>
    public static readonly NodeKind LiteralExpr = 201;

    /// <summary>
    ///     二元表达式
    /// </summary>
    public static readonly NodeKind BinaryExpr = 202;

    /// <summary>
    ///     一元表达式
    /// </summary>
    public static readonly NodeKind UnaryExpr = 203;

    /// <summary>
    ///     赋值表达式
    /// </summary>
    public static readonly NodeKind AssignmentExpr = 204;

    /// <summary>
    ///     调用表达式
    /// </summary>
    public static readonly NodeKind CallExpr = 205;

    /// <summary>
    ///     成员表达式
    /// </summary>
    public static readonly NodeKind MemberExpr = 206;

    /// <summary>
    ///     属性访问表达式
    /// </summary>
    public static readonly NodeKind PropertyAccessExpr = 207;

    /// <summary>
    ///     元素访问表达式
    /// </summary>
    public static readonly NodeKind ElementAccessExpr = 208;

    /// <summary>
    ///     箭头函数表达式
    /// </summary>
    public static readonly NodeKind ArrowFunctionExpr = 209;

    /// <summary>
    ///     数组字面量
    /// </summary>
    public static readonly NodeKind ArrayLiteral = 210;

    /// <summary>
    ///     对象字面量
    /// </summary>
    public static readonly NodeKind ObjectLiteral = 211;

    /// <summary>
    ///     this 表达式
    /// </summary>
    public static readonly NodeKind ThisExpr = 212;

    /// <summary>
    ///     super 表达式
    /// </summary>
    public static readonly NodeKind SuperExpr = 213;

    /// <summary>
    ///     函数表达式
    /// </summary>
    public static readonly NodeKind FunctionExpr = 214;

    /// <summary>
    ///     类表达式
    /// </summary>
    public static readonly NodeKind ClassExpr = 215;

    /// <summary>
    ///     条件表达式
    /// </summary>
    public static readonly NodeKind ConditionalExpr = 216;

    /// <summary>
    ///     new 表达式
    /// </summary>
    public static readonly NodeKind NewExpr = 217;

    /// <summary>
    ///     模板字面量插值
    /// </summary>
    public static readonly NodeKind TemplateLiteral = 218;

    /// <summary>
    ///     展开元素
    /// </summary>
    public static readonly NodeKind SpreadElement = 219;

    /// <summary>
    ///     yield 表达式
    /// </summary>
    public static readonly NodeKind YieldExpr = 220;

    /// <summary>
    ///     typeof 表达式
    /// </summary>
    public static readonly NodeKind TypeofExpr = 221;

    /// <summary>
    ///     instanceof 表达式
    /// </summary>
    public static readonly NodeKind InstanceofExpr = 222;

    #endregion

    #region 声明节点

    /// <summary>
    ///     编译单元
    /// </summary>
    public static readonly NodeKind CompilationUnit = 300;

    /// <summary>
    ///     导入声明
    /// </summary>
    public static readonly NodeKind ImportDecl = 301;

    /// <summary>
    ///     导出声明
    /// </summary>
    public static readonly NodeKind ExportDecl = 302;

    /// <summary>
    ///     变量声明
    /// </summary>
    public static readonly NodeKind VariableDecl = 303;

    /// <summary>
    ///     函数声明
    /// </summary>
    public static readonly NodeKind FunctionDecl = 304;

    /// <summary>
    ///     类声明
    /// </summary>
    public static readonly NodeKind ClassDecl = 305;

    /// <summary>
    ///     接口声明
    /// </summary>
    public static readonly NodeKind InterfaceDecl = 306;

    /// <summary>
    ///     类型别名声明
    /// </summary>
    public static readonly NodeKind TypeAliasDecl = 307;

    /// <summary>
    ///     枚举声明
    /// </summary>
    public static readonly NodeKind EnumDecl = 308;

    /// <summary>
    ///     枚举成员
    /// </summary>
    public static readonly NodeKind EnumMember = 309;

    /// <summary>
    ///     命名空间声明
    /// </summary>
    public static readonly NodeKind NamespaceDecl = 310;

    #endregion

    #region 类型节点

    /// <summary>
    ///     类型注解
    /// </summary>
    public static readonly NodeKind TypeAnnotation = 400;

    /// <summary>
    ///     原始类型
    /// </summary>
    public static readonly NodeKind PrimitiveType = 401;

    /// <summary>
    ///     联合类型
    /// </summary>
    public static readonly NodeKind UnionType = 402;

    /// <summary>
    ///     数组类型
    /// </summary>
    public static readonly NodeKind ArrayType = 403;

    #endregion

    #region 通用节点

    /// <summary>
    ///     函数参数
    /// </summary>
    public static readonly NodeKind Parameter = 500;

    /// <summary>
    ///     对象属性
    /// </summary>
    public static readonly NodeKind Property = 501;

    #endregion
}
