using Oak.Diagnostics;
using Oak.Parsing;

namespace Oak.GraphQL;

public abstract class GqlNode
{
    public abstract string ToString();
}

public sealed class GqlSchema : GqlNode
{
    public IReadOnlyList<GqlTypeDefinition> TypeDefinitions { get; }
    public IReadOnlyList<GqlDirectiveDefinition> DirectiveDefinitions { get; }

    public GqlSchema(IReadOnlyList<GqlTypeDefinition> typeDefinitions, IReadOnlyList<GqlDirectiveDefinition> directiveDefinitions)
    {
        TypeDefinitions = typeDefinitions;
        DirectiveDefinitions = directiveDefinitions;
    }

    public override string ToString()
    {
        return string.Join("\n", TypeDefinitions.Cast<object>().Concat(DirectiveDefinitions));
    }
}

public sealed class GqlTypeDefinition : GqlNode
{
    public string Name { get; }
    public string Kind { get; }
    public IReadOnlyList<GqlFieldDefinition> Fields { get; }
    public IReadOnlyList<string> Implements { get; }
    public IReadOnlyList<GqlDirective> Directives { get; }

    public GqlTypeDefinition(string name, string kind, IReadOnlyList<GqlFieldDefinition> fields, IReadOnlyList<string> implements, IReadOnlyList<GqlDirective> directives)
    {
        Name = name;
        Kind = kind;
        Fields = fields;
        Implements = implements;
        Directives = directives;
    }

    public override string ToString()
    {
        var impl = Implements.Count > 0 ? $" implements {string.Join(" & ", Implements)}" : "";
        var fields = string.Join("\n  ", Fields);
        return $"{Kind} {Name}{impl} {{\n  {fields}\n}}";
    }
}

public sealed class GqlFieldDefinition : GqlNode
{
    public string Name { get; }
    public IReadOnlyList<GqlInputValueDefinition> Arguments { get; }
    public GqlTypeRef Type { get; }
    public IReadOnlyList<GqlDirective> Directives { get; }

    public GqlFieldDefinition(string name, IReadOnlyList<GqlInputValueDefinition> arguments, GqlTypeRef type, IReadOnlyList<GqlDirective> directives)
    {
        Name = name;
        Arguments = arguments;
        Type = type;
        Directives = directives;
    }

    public override string ToString()
    {
        var args = Arguments.Count > 0 ? $"({string.Join(", ", Arguments)})" : "";
        return $"{Name}{args}: {Type}";
    }
}

public sealed class GqlInputValueDefinition : GqlNode
{
    public string Name { get; }
    public GqlTypeRef Type { get; }
    public GqlValue? DefaultValue { get; }
    public IReadOnlyList<GqlDirective> Directives { get; }

    public GqlInputValueDefinition(string name, GqlTypeRef type, GqlValue? defaultValue, IReadOnlyList<GqlDirective> directives)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        Directives = directives;
    }

    public override string ToString()
    {
        var def = DefaultValue is not null ? $" = {DefaultValue}" : "";
        return $"{Name}: {Type}{def}";
    }
}

public sealed class GqlDirectiveDefinition : GqlNode
{
    public string Name { get; }
    public IReadOnlyList<GqlInputValueDefinition> Arguments { get; }
    public IReadOnlyList<GqlDirectiveLocation> Locations { get; }
    public bool Repeatable { get; }

    public GqlDirectiveDefinition(string name, IReadOnlyList<GqlInputValueDefinition> arguments, IReadOnlyList<GqlDirectiveLocation> locations, bool repeatable)
    {
        Name = name;
        Arguments = arguments;
        Locations = locations;
        Repeatable = repeatable;
    }

    public override string ToString()
    {
        var args = Arguments.Count > 0 ? $"({string.Join(", ", Arguments)})" : "";
        var rep = Repeatable ? " repeatable" : "";
        var locs = string.Join(" | ", Locations);
        return $"directive @{Name}{args}{rep} on {locs}";
    }
}

public enum GqlDirectiveLocation
{
    Query,
    Mutation,
    Subscription,
    Field,
    FragmentDefinition,
    FragmentSpread,
    InlineFragment,
    VariableDefinition,
    Schema,
    Scalar,
    Object,
    FieldDefinition,
    ArgumentDefinition,
    Interface,
    Union,
    Enum,
    EnumValue,
    InputObject,
    InputFieldDefinition
}

public sealed class GqlDirective : GqlNode
{
    public string Name { get; }
    public IReadOnlyList<(string Name, GqlValue Value)> Arguments { get; }

    public GqlDirective(string name, IReadOnlyList<(string Name, GqlValue Value)> arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    public override string ToString()
    {
        var args = Arguments.Count > 0 ? $"({string.Join(", ", Arguments.Select(a => $"{a.Name}: {a.Value}"))})" : "";
        return $"@{Name}{args}";
    }
}

public abstract class GqlTypeRef : GqlNode;

public sealed class GqlNamedType : GqlTypeRef
{
    public string Name { get; }

    public GqlNamedType(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}

public sealed class GqlNonNullType : GqlTypeRef
{
    public GqlTypeRef Inner { get; }

    public GqlNonNullType(GqlTypeRef inner)
    {
        Inner = inner;
    }

    public override string ToString()
    {
        return $"{Inner}!";
    }
}

public sealed class GqlListType : GqlTypeRef
{
    public GqlTypeRef ElementType { get; }

    public GqlListType(GqlTypeRef elementType)
    {
        ElementType = elementType;
    }

    public override string ToString()
    {
        return $"[{ElementType}]";
    }
}

public abstract class GqlValue : GqlNode;

public sealed class GqlIntValue : GqlValue
{
    public string Value { get; }

    public GqlIntValue(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}

public sealed class GqlFloatValue : GqlValue
{
    public string Value { get; }

    public GqlFloatValue(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}

public sealed class GqlStringValue : GqlValue
{
    public string Value { get; }

    public GqlStringValue(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"\"{Value}\"";
    }
}

public sealed class GqlBooleanValue : GqlValue
{
    public bool Value { get; }

    public GqlBooleanValue(bool value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value ? "true" : "false";
    }
}

public sealed class GqlNullValue : GqlValue
{
    public static GqlNullValue Instance { get; } = new();

    public override string ToString()
    {
        return "null";
    }
}

public sealed class GqlEnumValue : GqlValue
{
    public string Value { get; }

    public GqlEnumValue(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}

public sealed class GqlListValue : GqlValue
{
    public IReadOnlyList<GqlValue> Values { get; }

    public GqlListValue(IReadOnlyList<GqlValue> values)
    {
        Values = values;
    }

    public override string ToString()
    {
        return $"[{string.Join(", ", Values)}]";
    }
}

public sealed class GqlObjectValue : GqlValue
{
    public IReadOnlyList<(string Name, GqlValue Value)> Fields { get; }

    public GqlObjectValue(IReadOnlyList<(string Name, GqlValue Value)> fields)
    {
        Fields = fields;
    }

    public override string ToString()
    {
        return $"{{{string.Join(", ", Fields.Select(f => $"{f.Name}: {f.Value}"))}}}";
    }
}
