using Oak.Diagnostics;
using Oak.Parsing;

namespace Oak.Protobuf;

public abstract class ProtoNode
{
    public abstract string ToString();
}

public sealed class ProtoFile : ProtoNode
{
    public string? Syntax { get; }
    public string? Package { get; }
    public IReadOnlyList<string> Imports { get; }
    public IReadOnlyList<ProtoOption> Options { get; }
    public IReadOnlyList<ProtoMessage> Messages { get; }
    public IReadOnlyList<ProtoEnum> Enums { get; }
    public IReadOnlyList<ProtoService> Services { get; }

    public ProtoFile(string? syntax, string? package, IReadOnlyList<string> imports, IReadOnlyList<ProtoOption> options, IReadOnlyList<ProtoMessage> messages, IReadOnlyList<ProtoEnum> enums, IReadOnlyList<ProtoService> services)
    {
        Syntax = syntax;
        Package = package;
        Imports = imports;
        Options = options;
        Messages = messages;
        Enums = enums;
        Services = services;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Syntax is not null) parts.Add($"syntax = \"{Syntax}\";");
        if (Package is not null) parts.Add($"package {Package};");
        foreach (var imp in Imports) parts.Add($"import \"{imp}\";");
        parts.AddRange(Messages.Select(m => m.ToString()!));
        parts.AddRange(Enums.Select(e => e.ToString()!));
        parts.AddRange(Services.Select(s => s.ToString()!));
        return string.Join("\n", parts);
    }
}

public sealed class ProtoMessage : ProtoNode
{
    public string Name { get; }
    public IReadOnlyList<ProtoField> Fields { get; }
    public IReadOnlyList<ProtoMessage> NestedMessages { get; }
    public IReadOnlyList<ProtoEnum> NestedEnums { get; }
    public IReadOnlyList<ProtoOneof> Oneofs { get; }
    public IReadOnlyList<ProtoMapField> MapFields { get; }
    public IReadOnlyList<ProtoReserved> Reserved { get; }
    public IReadOnlyList<ProtoOption> Options { get; }

    public ProtoMessage(string name, IReadOnlyList<ProtoField> fields, IReadOnlyList<ProtoMessage> nestedMessages, IReadOnlyList<ProtoEnum> nestedEnums, IReadOnlyList<ProtoOneof> oneofs, IReadOnlyList<ProtoMapField> mapFields, IReadOnlyList<ProtoReserved> reserved, IReadOnlyList<ProtoOption> options)
    {
        Name = name;
        Fields = fields;
        NestedMessages = nestedMessages;
        NestedEnums = nestedEnums;
        Oneofs = oneofs;
        MapFields = mapFields;
        Reserved = reserved;
        Options = options;
    }

    public override string ToString()
    {
        return $"message {Name} {{ ... }}";
    }
}

public sealed class ProtoField : ProtoNode
{
    public string Label { get; }
    public string Type { get; }
    public string Name { get; }
    public int Number { get; }
    public IReadOnlyList<ProtoOption> Options { get; }

    public ProtoField(string label, string type, string name, int number, IReadOnlyList<ProtoOption> options)
    {
        Label = label;
        Type = type;
        Name = name;
        Number = number;
        Options = options;
    }

    public override string ToString()
    {
        var opts = Options.Count > 0 ? $" [{string.Join(", ", Options)}]" : "";
        return $"{Label} {Type} {Name} = {Number}{opts};";
    }
}

public sealed class ProtoMapField : ProtoNode
{
    public string KeyType { get; }
    public string ValueType { get; }
    public string Name { get; }
    public int Number { get; }

    public ProtoMapField(string keyType, string valueType, string name, int number)
    {
        KeyType = keyType;
        ValueType = valueType;
        Name = name;
        Number = number;
    }

    public override string ToString()
    {
        return $"map<{KeyType}, {ValueType}> {Name} = {Number};";
    }
}

public sealed class ProtoOneof : ProtoNode
{
    public string Name { get; }
    public IReadOnlyList<ProtoField> Fields { get; }

    public ProtoOneof(string name, IReadOnlyList<ProtoField> fields)
    {
        Name = name;
        Fields = fields;
    }

    public override string ToString()
    {
        return $"oneof {Name} {{ ... }}";
    }
}

public sealed class ProtoEnum : ProtoNode
{
    public string Name { get; }
    public IReadOnlyList<ProtoEnumValue> Values { get; }

    public ProtoEnum(string name, IReadOnlyList<ProtoEnumValue> values)
    {
        Name = name;
        Values = values;
    }

    public override string ToString()
    {
        return $"enum {Name} {{ ... }}";
    }
}

public sealed class ProtoEnumValue : ProtoNode
{
    public string Name { get; }
    public int Number { get; }

    public ProtoEnumValue(string name, int number)
    {
        Name = name;
        Number = number;
    }

    public override string ToString()
    {
        return $"{Name} = {Number};";
    }
}

public sealed class ProtoService : ProtoNode
{
    public string Name { get; }
    public IReadOnlyList<ProtoRpc> Methods { get; }

    public ProtoService(string name, IReadOnlyList<ProtoRpc> methods)
    {
        Name = name;
        Methods = methods;
    }

    public override string ToString()
    {
        return $"service {Name} {{ ... }}";
    }
}

public sealed class ProtoRpc : ProtoNode
{
    public string Name { get; }
    public string InputType { get; }
    public bool InputStream { get; }
    public string OutputType { get; }
    public bool OutputStream { get; }

    public ProtoRpc(string name, string inputType, bool inputStream, string outputType, bool outputStream)
    {
        Name = name;
        InputType = inputType;
        InputStream = inputStream;
        OutputType = outputType;
        OutputStream = outputStream;
    }

    public override string ToString()
    {
        var inStr = InputStream ? "stream " : "";
        var outStr = OutputStream ? "stream " : "";
        return $"rpc {Name}({inStr}{InputType}) returns ({outStr}{OutputType});";
    }
}

public sealed class ProtoOption : ProtoNode
{
    public string Name { get; }
    public string Value { get; }

    public ProtoOption(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public override string ToString()
    {
        return $"{Name} = {Value}";
    }
}

public sealed class ProtoReserved : ProtoNode
{
    public IReadOnlyList<string> Names { get; }
    public IReadOnlyList<(int Start, int End)> Ranges { get; }

    public ProtoReserved(IReadOnlyList<string> names, IReadOnlyList<(int Start, int End)> ranges)
    {
        Names = names;
        Ranges = ranges;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        parts.AddRange(Names);
        parts.AddRange(Ranges.Select(r => r.Start == r.End ? r.Start.ToString() : $"{r.Start} to {r.End}"));
        return $"reserved {string.Join(", ", parts)};";
    }
}
