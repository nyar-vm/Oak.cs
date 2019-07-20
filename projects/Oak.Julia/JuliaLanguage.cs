using Oak.Syntax;

namespace Oak.Julia;

public sealed class JuliaLanguage : Language
{
    public override string Name => "Julia";
    public bool MultipleDispatchEnabled { get; init; } = true;
    public bool MacroEnabled { get; init; } = true;
    public bool CoroutineEnabled { get; init; }
    public bool TabularEnabled { get; init; }

    public static JuliaLanguage Default => new();
    public static JuliaLanguage Julia17 => new()
    {
        MultipleDispatchEnabled = true,
        MacroEnabled = true,
        CoroutineEnabled = true
    };
}
