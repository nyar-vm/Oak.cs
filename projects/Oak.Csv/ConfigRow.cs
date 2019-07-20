using System.Collections;
using System.Globalization;

namespace Oak.Csv;

/// <summary>
///     配置表数据行，提供按字段名访问和类型转换
/// </summary>
public sealed class ConfigRow
{
    private readonly Dictionary<string, int> _fieldIndex;
    private readonly IReadOnlyList<ConfigField> _fields;
    private readonly IReadOnlyList<string> _values;

    /// <summary>
    ///     初始化配置表数据行
    /// </summary>
    public ConfigRow(IReadOnlyList<ConfigField> fields, IReadOnlyList<string> values)
    {
        _fields = fields;
        _values = values;
        _fieldIndex = new Dictionary<string, int>(fields.Count);

        for (var i = 0; i < fields.Count; i++) _fieldIndex[fields[i].Name] = i;
    }

    /// <summary>
    ///     根据字段名获取原始字符串值
    /// </summary>
    public string? GetString(string fieldName)
    {
        if (!_fieldIndex.TryGetValue(fieldName, out var index)) return null;

        if (index >= _values.Count) return null;

        return _values[index];
    }

    /// <summary>
    ///     根据字段名获取类型转换后的值
    /// </summary>
    public T GetField<T>(string fieldName)
    {
        if (!_fieldIndex.TryGetValue(fieldName, out var index)) throw new KeyNotFoundException($"字段不存在：{fieldName}");

        var field = _fields[index];
        var rawValue = index < _values.Count ? _values[index] : string.Empty;

        return (T)ConvertValue(rawValue, field.FieldType, typeof(T));
    }

    /// <summary>
    ///     安全地根据字段名获取类型转换后的值
    /// </summary>
    public bool TryGetField<T>(string fieldName, out T value)
    {
        try
        {
            value = GetField<T>(fieldName);
            return true;
        }
        catch (FormatException)
        {
            value = default!;
            return false;
        }
        catch (KeyNotFoundException)
        {
            value = default!;
            return false;
        }
        catch (InvalidOperationException)
        {
            value = default!;
            return false;
        }
    }

    private static object ConvertValue(string rawValue, TableFieldType fieldType, Type targetType)
    {
        if (IsListType(fieldType)) return ConvertListValue(rawValue, fieldType, targetType);

        return ConvertPrimitiveValue(rawValue, fieldType, targetType);
    }

    private static bool IsListType(TableFieldType fieldType)
    {
        return fieldType.ToString().StartsWith('[');
    }

    private static TableFieldType GetListElementType(TableFieldType listFieldType)
    {
        var str = listFieldType.ToString();
        var inner = str[1..^1];
        var depth = 0;

        for (var i = 0; i < inner.Length; i++)
            if (inner[i] == '[')
                depth++;
            else if (inner[i] == ']')
                depth--;
            else if (inner[i] == ';' && depth == 0) return new TableFieldTypeParser().Parse(inner[..i].Trim());

        return new TableFieldTypeParser().Parse(inner.Trim());
    }

    private static object ConvertListValue(string rawValue, TableFieldType listFieldType, Type targetType)
    {
        var elementType = GetListElementType(listFieldType);
        var parts = string.IsNullOrEmpty(rawValue) ? [] : rawValue.Split(',');

        if (targetType.IsArray)
        {
            var elementClrType = targetType.GetElementType()!;
            var array = Array.CreateInstance(elementClrType, parts.Length);

            for (var i = 0; i < parts.Length; i++)
                array.SetValue(ConvertPrimitiveValue(parts[i].Trim(), elementType, elementClrType), i);

            return array;
        }

        var genericArgs = targetType.GetGenericArguments();

        if (genericArgs.Length == 1)
        {
            var list = (IList)Activator.CreateInstance(targetType)!;

            foreach (var part in parts) list.Add(ConvertPrimitiveValue(part.Trim(), elementType, genericArgs[0]));

            return list;
        }

        throw new InvalidOperationException($"不支持的列表类型：{targetType.Name}");
    }

    private static object ConvertPrimitiveValue(string rawValue, TableFieldType fieldType, Type targetType)
    {
        ValidateTypeCompatibility(fieldType, targetType);

        if (fieldType == TableFieldType.I32) return int.Parse(rawValue, CultureInfo.InvariantCulture);

        if (fieldType == TableFieldType.I64) return long.Parse(rawValue, CultureInfo.InvariantCulture);

        if (fieldType == TableFieldType.F32) return float.Parse(rawValue, CultureInfo.InvariantCulture);

        if (fieldType == TableFieldType.F64) return double.Parse(rawValue, CultureInfo.InvariantCulture);

        if (fieldType == TableFieldType.Bool) return bool.Parse(rawValue);

        if (fieldType == TableFieldType.String) return rawValue;

        if (fieldType.ToString().StartsWith('&')) return rawValue;

        throw new InvalidOperationException($"不支持的字段类型：{fieldType}");
    }

    private static void ValidateTypeCompatibility(TableFieldType fieldType, Type targetType)
    {
        Type? expectedType = null;

        if (fieldType == TableFieldType.I32)
            expectedType = typeof(int);
        else if (fieldType == TableFieldType.I64)
            expectedType = typeof(long);
        else if (fieldType == TableFieldType.F32)
            expectedType = typeof(float);
        else if (fieldType == TableFieldType.F64)
            expectedType = typeof(double);
        else if (fieldType == TableFieldType.Bool)
            expectedType = typeof(bool);
        else if (fieldType == TableFieldType.String)
            expectedType = typeof(string);
        else if (fieldType.ToString().StartsWith('&')) expectedType = typeof(string);

        if (expectedType != null && targetType != expectedType)
            throw new InvalidOperationException(
                $"类型不兼容：字段类型为 {fieldType}，期望 {expectedType.Name}，实际 {targetType.Name}");
    }
}