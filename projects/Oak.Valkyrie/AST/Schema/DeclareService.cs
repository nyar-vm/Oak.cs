using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Schema;

/// <summary>
///     服务声明，定义 RPC 或 REST 风格的服务接口
/// </summary>
/// <para>示例：</para>
/// <code>
/// class UserInfo {
///     name: utf8,
///     email: utf8
/// }
/// service UserService {
///     [get("/user/{id}")]
///     user_info(id: i32) -> UserInfo;
///
///     [post("/user")]
///     user_create(info: UserInfo) -> result<i32>;
/// }
/// </code>
public sealed record DeclareService : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     服务名称
    /// </summary>
    public IdentifierNode? Name { get; init; } = new();

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? Body { get; init; } = null;
}
