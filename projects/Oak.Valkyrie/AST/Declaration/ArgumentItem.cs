using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     属性参数键值对，表示 <c>#[attr(key = "value")]</c> 中的单个参数
/// </summary>
/// <para>示例：</para>
/// <code>
/// [serialize(name = "player_name")]
/// let playerName: utf8;
/// </code>
public sealed record ArgumentItem
{
    /// <summary>
    ///     参数键名
    /// </summary>
    public IDeclarationNode? Key { get; init; } = null;

    /// <summary>
    ///     参数值，默认为 <c>"true"</c>（支持无值属性如 <c>[hidden]</c>）
    /// </summary>
    public TermNode Value { get; init; } = null;
}