using Oak.Syntax;

namespace Oak.C;

public sealed class CLanguage : Language
{
    public override string Name => "C";
    public bool C11Enabled { get; init; } = true;
    public bool C99Enabled { get; init; } = true;

    public static CLanguage Default => new();
}