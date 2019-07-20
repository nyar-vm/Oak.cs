using Oak.Data;

namespace Oak.Ini;

public static class IniValueExtensions
{
    public static SerdeValue ToSerdeValue(this IniParseResult result)
    {
        var fields = new Dictionary<string, SerdeValue>();

        foreach (var (key, value) in result.GlobalEntries)
        {
            fields[key] = ClassifyValue(value);
        }

        foreach (var section in result.Sections)
        {
            var sectionFields = new Dictionary<string, SerdeValue>();

            foreach (var (key, value) in section.Entries)
            {
                sectionFields[key] = ClassifyValue(value);
            }

            fields[section.Name] = SerdeValue.Object(sectionFields);
        }

        return SerdeValue.Object(fields);
    }

    private static SerdeValue ClassifyValue(string value)
    {
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return SerdeValue.Boolean(true);
        }

        if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return SerdeValue.Boolean(false);
        }

        if (long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var intVal))
        {
            return SerdeValue.Integer(intVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
        {
            return SerdeValue.Decimal(floatVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return SerdeValue.String(value);
    }

    public static IniParseResult ToIniParseResult(this SerdeValue serdeValue)
    {
        var globalEntries = new Dictionary<string, string>();
        var sections = new List<IniSection>();

        foreach (var (key, value) in serdeValue.Fields ?? [])
        {
            if (value.Type == SerdeValueType.Object)
            {
                var section = new IniSection { Name = key };

                foreach (var (entryKey, entryValue) in value.Fields ?? [])
                {
                    section.Entries[entryKey] = ConvertToString(entryValue);
                }

                sections.Add(section);
            }
            else
            {
                globalEntries[key] = ConvertToString(value);
            }
        }

        return new IniParseResult
        {
            GlobalEntries = globalEntries,
            Sections = sections
        };
    }

    private static string ConvertToString(SerdeValue value)
    {
        return value.Type switch
        {
            SerdeValueType.Boolean => value.GetBoolean().ToString().ToLowerInvariant(),
            SerdeValueType.Integer => value.GetIntegerString() ?? "",
            SerdeValueType.Decimal => value.GetDecimalString() ?? "",
            SerdeValueType.String => value.GetString() ?? "",
            SerdeValueType.Null => "",
            _ => value.ToString() ?? ""
        };
    }
}
