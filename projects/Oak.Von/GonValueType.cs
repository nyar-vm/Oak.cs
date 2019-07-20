namespace Oak.Von;

/// <summary>
///     Gon 值类型枚举（已弃用，请使用 Oak.Data.DataValueType）
/// </summary>
[Obsolete("请使用 Oak.Data.DataValueType")]
public enum GonValueType
{
    Null = 0,
    Boolean = 1,
    Integer = 2,
    Decimal = 3,
    String = 4,
    Array = 5,
    Object = 6
}
