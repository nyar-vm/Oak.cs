using Oak.Syntax;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Template;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.ECS;

/// <summary>
///     ECS 系统声明，定义对满足条件的实体集合执行的操作逻辑
/// </summary>
/// <para>示例：</para>
/// <code>
/// system movement {
///     query all [Position, Velocity];
///
///     micro run(param: f32) {
///         for each entity {
///             entity.position += entity.velocity * param;
///         }
///     }
/// }
/// </code>
public sealed record DeclareSystem : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     系统名称
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();
    
    /// <summary>
    ///     查询表达式列表，定义系统操作的实体范围
    /// </summary>
    public IReadOnlyList<QueryExpr> Queries { get; init; } = [];

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? Body { get; init; } = null;
}
