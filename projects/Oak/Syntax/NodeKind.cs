namespace Oak.Syntax;

public readonly struct NodeKind : IEquatable<NodeKind>
{
    public int Value { get; }

    public NodeKind(int value)
    {
        Value = value;
    }

    public bool Equals(NodeKind other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is NodeKind other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(NodeKind left, NodeKind right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NodeKind left, NodeKind right)
    {
        return !left.Equals(right);
    }

    public static implicit operator NodeKind(int value)
    {
        return new NodeKind(value);
    }

    public static implicit operator int(NodeKind kind)
    {
        return kind.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}