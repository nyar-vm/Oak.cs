using Oak.Syntax;

namespace Oak.Python;

public sealed class PythonLanguage : Language
{
    public override string Name => "Python";
    public bool Python3Enabled { get; init; } = true;
    public bool TypeHintsEnabled { get; init; } = true;
    public bool WalrusOperatorEnabled { get; init; } = true;
    public bool MatchStatementEnabled { get; init; }

    public static PythonLanguage Default => new();
    public static PythonLanguage Python310 => new() { MatchStatementEnabled = true };
}