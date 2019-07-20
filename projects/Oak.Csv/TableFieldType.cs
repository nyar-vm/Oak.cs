namespace Oak.Csv;

/// <summary>
///     配置表字段类型种类
/// </summary>
public enum TableFieldTypeKind
{
    I32,
    I64,
    F32,
    F64,
    Bool,
    String,
    List,
    FixedList,
    Reference
}

/// <summary>
///     配置表字段类型
/// </summary>
public abstract class TableFieldType
{
    /// <summary>
    ///     32 位有符号整数
    /// </summary>
    public static readonly TableFieldType I32 = new PrimitiveType(TableFieldTypeKind.I32, "i32");

    /// <summary>
    ///     64 位有符号整数
    /// </summary>
    public static readonly TableFieldType I64 = new PrimitiveType(TableFieldTypeKind.I64, "i64");

    /// <summary>
    ///     32 位浮点数
    /// </summary>
    public static readonly TableFieldType F32 = new PrimitiveType(TableFieldTypeKind.F32, "f32");

    /// <summary>
    ///     64 位浮点数
    /// </summary>
    public static readonly TableFieldType F64 = new PrimitiveType(TableFieldTypeKind.F64, "f64");

    /// <summary>
    ///     布尔值
    /// </summary>
    public static readonly TableFieldType Bool = new PrimitiveType(TableFieldTypeKind.Bool, "bool");

    /// <summary>
    ///     字符串
    /// </summary>
    public static readonly TableFieldType String = new PrimitiveType(TableFieldTypeKind.String, "string");

    /// <summary>
    ///     类型种类
    /// </summary>
    public abstract TableFieldTypeKind Kind { get; }

    /// <summary>
    ///     创建列表类型
    /// </summary>
    public static TableFieldType List(TableFieldType elementType)
    {
        return new ListType(elementType);
    }

    /// <summary>
    ///     创建固定大小数组类型
    /// </summary>
    public static TableFieldType FixedList(TableFieldType elementType, int size)
    {
        return new FixedListType(elementType, size);
    }

    /// <summary>
    ///     创建引用类型
    /// </summary>
    public static TableFieldType Reference(string targetTable)
    {
        return new ReferenceType(targetTable);
    }

    /// <summary>
    ///     获取列表元素类型
    /// </summary>
    public TableFieldType? GetListElementType()
    {
        return this switch
        {
            ListType l => l.ElementType,
            FixedListType f => f.ElementType,
            _ => null
        };
    }

    /// <summary>
    ///     获取固定数组大小
    /// </summary>
    public int? GetFixedListSize()
    {
        return this is FixedListType f ? f.Size : null;
    }

    /// <summary>
    ///     获取引用目标表名
    /// </summary>
    public string? GetReferenceTarget()
    {
        return this is ReferenceType r ? r.TargetTable : null;
    }

    private sealed class PrimitiveType(TableFieldTypeKind kind, string name) : TableFieldType
    {
        public override TableFieldTypeKind Kind { get; } = kind;

        public string Name { get; } = name;

        public override string ToString()
        {
            return Name;
        }
    }

    private sealed class ListType(TableFieldType elementType) : TableFieldType
    {
        public override TableFieldTypeKind Kind { get; } = TableFieldTypeKind.List;

        public TableFieldType ElementType { get; } = elementType;

        public override string ToString()
        {
            return $"[{ElementType}]";
        }
    }

    private sealed class FixedListType(TableFieldType elementType, int size) : TableFieldType
    {
        public override TableFieldTypeKind Kind { get; } = TableFieldTypeKind.FixedList;

        public TableFieldType ElementType { get; } = elementType;

        public int Size { get; } = size;

        public override string ToString()
        {
            return $"[{ElementType}; {Size}]";
        }
    }

    private sealed class ReferenceType(string targetTable) : TableFieldType
    {
        public override TableFieldTypeKind Kind { get; } = TableFieldTypeKind.Reference;

        public string TargetTable { get; } = targetTable;

        public override string ToString()
        {
            return $"&{TargetTable}";
        }
    }
}