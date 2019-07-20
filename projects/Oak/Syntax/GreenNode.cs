using System.Text;

namespace Oak.Syntax;

/// <summary>
///     Green 语法树节点抽象基类
/// </summary>
public abstract class GreenNode
{
    /// <summary>
    ///     节点类型标识
    /// </summary>
    public abstract NodeKind Kind { get; }

    /// <summary>
    ///     节点在源码中的总字符跨度
    /// </summary>
    public abstract int Width { get; }

    /// <summary>
    ///     子节点数量
    /// </summary>
    public abstract int ChildCount { get; }

    /// <summary>
    ///     是否为叶子节点
    /// </summary>
    public bool IsLeaf => ChildCount == 0;

    /// <summary>
    ///     总文本长度（用于预分配 StringBuilder）
    /// </summary>
    public virtual int FullWidth => Width;

    /// <summary>
    ///     获取所有子节点（惰性枚举）
    /// </summary>
    public IEnumerable<GreenNode> Children
    {
        get
        {
            for (var i = 0; i < ChildCount; i++)
            {
                var child = GetChild(i);
                if (child is not null)
                {
                    yield return child;
                }
            }
        }
    }

    /// <summary>
    ///     获取指定索引的子节点
    /// </summary>
    public abstract GreenNode? GetChild(int index);

    /// <summary>
    ///     将节点文本写入 TextWriter（虚拟方法，遍历所有子节点写入）
    /// </summary>
    public virtual void WriteTo(TextWriter writer)
    {
        for (var i = 0; i < ChildCount; i++)
        {
            var child = GetChild(i);
            child?.WriteTo(writer);
        }
    }

    /// <summary>
    ///     批量写入 StringBuilder
    /// </summary>
    public void WriteTo(StringBuilder sb)
    {
        WriteToStringBuilder(sb);
    }

    /// <summary>
    ///     将整个子树文本写入 StringBuilder（预分配容量优化）
    /// </summary>
    protected virtual void WriteToStringBuilder(StringBuilder sb)
    {
        sb.EnsureCapacity(sb.Length + Width);
        for (var i = 0; i < ChildCount; i++)
        {
            var child = GetChild(i);
            if (child is GreenLeafNode leaf)
            {
                if (leaf.Text is not null)
                {
                    sb.Append(leaf.Text);
                }
            }
            else
            {
                child?.WriteToStringBuilder(sb);
            }
        }
    }

    /// <summary>
    ///     获取整棵子树的文本内容
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder(Width);
        WriteToStringBuilder(sb);
        return sb.ToString();
    }

    /// <summary>
    ///     判断从指定偏移量开始的节点是否与目标范围重叠
    /// </summary>
    public bool OverlapsWith(TextSpan span, int offset)
    {
        var nodeEnd = offset + Width;
        return span.Start < nodeEnd && offset < span.End;
    }

    /// <summary>
    ///     判断从指定偏移量开始的节点是否被目标范围完全包含
    /// </summary>
    public bool IsContainedBy(TextSpan span, int offset)
    {
        return span.Start <= offset && span.End >= offset + Width;
    }
}
