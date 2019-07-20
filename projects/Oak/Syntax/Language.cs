namespace Oak.Syntax;

public abstract class Language
{
    public abstract string Name { get; }

    public override string ToString()
    {
        return Name;
    }
}