namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     Channel 条件编译声明，按发布渠道（如 <c>"debug"</c>、<c>"release"</c>、<c>"mobile"</c>）包含不同的声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// channel "debug" {
///     fn assert(condition: bool, message: utf8) { ... }
/// }
///
/// channel "mobile" {
///     import "mobile/gestures";
/// }
/// </code>
public sealed record ChannelConditionalDecl : ValkyrieNode
{
    /// <summary>
    ///     Channel 名称（如 <c>"debug"</c>、<c>"release"</c>、<c>"mobile"</c>）
    /// </summary>
    public string ChannelName { get; init; } = string.Empty;

    /// <summary>
    ///     条件分支内的声明列表
    /// </summary>
    public IReadOnlyList<ValkyrieNode> Declarations { get; init; } = [];
}
