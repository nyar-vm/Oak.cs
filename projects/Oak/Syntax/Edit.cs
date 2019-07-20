using Oak.Syntax;
namespace Oak.Syntax;

public readonly struct Edit
{
    public TextSpan OldSpan { get; }

    public string NewText { get; }

    public Edit(TextSpan oldSpan, string newText)
    {
        OldSpan = oldSpan;
        NewText = newText;
    }

    public int Delta => NewText.Length - OldSpan.Length;

    public override string ToString()
    {
        return $"Replace {OldSpan} with \"{NewText}\"";
    }
}