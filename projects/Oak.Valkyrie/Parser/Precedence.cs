namespace Oak.Valkyrie.Parser;

/// <summary>
///     Pratt 解析器运算符优先级常量。
///     值越大绑定越紧。
/// </summary>
internal static class Precedence
{
    /// <summary>最低优先级（非运算符 Token）</summary>
    public const int Lowest = 0;

    /// <summary>赋值运算符 = += -= *= /= %= 等</summary>
    public const int Assignment = 10;

    /// <summary>逻辑或 ||</summary>
    public const int LogicalOr = 20;

    /// <summary>逻辑与 &amp;&amp;</summary>
    public const int LogicalAnd = 30;

    /// <summary>按位或 |</summary>
    public const int BitwiseOr = 35;

    /// <summary>按位异或 ^</summary>
    public const int BitwiseXor = 37;

    /// <summary>按位与 &amp;</summary>
    public const int BitwiseAnd = 40;

    /// <summary>相等性 == !=</summary>
    public const int Equality = 50;

    /// <summary>比较 > < >= <=</summary>
    public const int Comparison = 60;

    /// <summary>移位 >></summary>
    public const int Shift = 70;

    /// <summary>加减 + -</summary>
    public const int Sum = 80;

    /// <summary>乘除取模 * / %</summary>
    public const int Product = 90;

    /// <summary>按位运算（合并位运算）</summary>
    public const int Bitwise = 45;

    /// <summary>前缀一元运算符 - ! ~ ++ --</summary>
    public const int Prefix = 100;

    /// <summary>后缀一元运算符 ++ --</summary>
    public const int Postfix = 110;

    /// <summary>函数调用 / 成员访问 / 索引 / ::</summary>
    public const int Call = 120;
}
