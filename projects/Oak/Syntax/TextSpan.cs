using Oak.Syntax;
namespace Oak.Syntax;

public readonly struct TextSpan : IEquatable<TextSpan>
{
    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;

    public TextSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public bool Equals(TextSpan other)
    {
        return Start == other.Start && Length == other.Length;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextSpan other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, Length);
    }

    public static bool operator ==(TextSpan left, TextSpan right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextSpan left, TextSpan right)
    {
        return !left.Equals(right);
    }

    public bool Contains(int position)
    {
        return position >= Start && position < End;
    }

    public bool OverlapsWith(TextSpan other)
    {
        return other.Start < End && Start < other.End;
    }

    public override string ToString()
    {
        return $"[{Start}..{End})";
    }
}