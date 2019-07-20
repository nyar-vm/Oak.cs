namespace Oak.Syntax;

public interface ISource
{
    char this[int index] { get; }

    int Length { get; }

    string Substring(Range range);
}

public sealed class StringSource : ISource
{
    public static readonly StringSource Empty = new(string.Empty);

    private readonly string _text;

    public StringSource(string text)
    {
        _text = text;
    }

    public char this[int index] => _text[index];

    public int Length => _text.Length;

    public string Substring(Range range)
    {
        return _text[range];
    }

    public override string ToString()
    {
        return _text;
    }
}