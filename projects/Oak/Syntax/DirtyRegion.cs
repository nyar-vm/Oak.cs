namespace Oak.Syntax;

/// <summary>
///     脏区域，表示源码中被编辑影响的范围
/// </summary>
public readonly struct DirtyRegion : IEquatable<DirtyRegion>
{
    /// <summary>
    ///     脏区域的起始位置
    /// </summary>
    public int Start { get; }

    /// <summary>
    ///     脏区域的长度
    /// </summary>
    public int Length { get; }

    /// <summary>
    ///     脏区域的结束位置（不含）
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    ///     创建指定范围的脏区域
    /// </summary>
    public DirtyRegion(int start, int length)
    {
        Start = start;
        Length = length;
    }

    /// <summary>
    ///     从 TextSpan 创建脏区域
    /// </summary>
    public static DirtyRegion FromSpan(TextSpan span)
    {
        return new DirtyRegion(span.Start, span.Length);
    }

    /// <summary>
    ///     从编辑操作计算脏区域
    /// </summary>
    public static DirtyRegion FromEdit(Edit edit)
    {
        var start = edit.OldSpan.Start;
        var length = Math.Max(edit.OldSpan.Length, edit.NewText.Length);
        return new DirtyRegion(start, length);
    }

    /// <summary>
    ///     脏区域是否与指定节点重叠
    /// </summary>
    public bool OverlapsWith(GreenNode node, int nodeOffset)
    {
        var nodeEnd = nodeOffset + node.Width;
        return Start < nodeEnd && nodeOffset < End;
    }

    /// <summary>
    ///     脏区域是否完全包含指定节点
    /// </summary>
    public bool Contains(GreenNode node, int nodeOffset)
    {
        return Start <= nodeOffset && End >= nodeOffset + node.Width;
    }

    /// <summary>
    ///     转换为 TextSpan
    /// </summary>
    public TextSpan ToSpan()
    {
        return new TextSpan(Start, Length);
    }

    /// <summary>
    ///     如果重叠则合并两个脏区域，返回合并后的区域
    /// </summary>
    public DirtyRegion Merge(DirtyRegion other)
    {
        var newStart = Math.Min(Start, other.Start);
        var newEnd = Math.Max(End, other.End);
        return new DirtyRegion(newStart, newEnd - newStart);
    }

    public bool Equals(DirtyRegion other)
    {
        return Start == other.Start && Length == other.Length;
    }

    public override bool Equals(object? obj)
    {
        return obj is DirtyRegion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, Length);
    }

    public static bool operator ==(DirtyRegion left, DirtyRegion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DirtyRegion left, DirtyRegion right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"[{Start}..{End})";
    }
}
