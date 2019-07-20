using Oak.Syntax;
namespace Oak.Syntax;

/// <summary>
///     语法树变更事件，描述编辑前后两棵树的差异
/// </summary>
public readonly struct TreeChangeEvent
{
    /// <summary>
    ///     编辑前的旧语法树
    /// </summary>
    public SyntaxTree OldTree { get; }

    /// <summary>
    ///     编辑后的新语法树
    /// </summary>
    public SyntaxTree NewTree { get; }

    /// <summary>
    ///     受影响的源文本范围
    /// </summary>
    public TextSpan ChangedSpan { get; }

    /// <summary>
    ///     被替换的绿树节点列表
    /// </summary>
    public IReadOnlyList<GreenNode> ReplacedNodes { get; }

    /// <summary>
    ///     触发此次变更的编辑操作
    /// </summary>
    public Edit SourceEdit { get; }

    public TreeChangeEvent(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        TextSpan changedSpan,
        IReadOnlyList<GreenNode> replacedNodes,
        Edit sourceEdit)
    {
        OldTree = oldTree;
        NewTree = newTree;
        ChangedSpan = changedSpan;
        ReplacedNodes = replacedNodes;
        SourceEdit = sourceEdit;
    }

    public override string ToString()
    {
        return $"TreeChange: {ChangedSpan}, {ReplacedNodes.Count} nodes replaced";
    }
}