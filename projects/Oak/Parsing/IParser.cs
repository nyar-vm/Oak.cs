namespace Oak.Parsing;

/// <summary>
///     通用解析器接口
/// </summary>
/// <typeparam name="TInput">输入类型</typeparam>
/// <typeparam name="TOutput">输出类型</typeparam>
public interface IParser<in TInput, out TOutput>
{
    /// <summary>
    ///     解析输入并返回结果
    /// </summary>
    TOutput Parse(TInput input);
}

/// <summary>
///     字符串解析器接口
/// </summary>
/// <typeparam name="TOutput">输出类型</typeparam>
public interface IStringParser<out TOutput> : IParser<string, TOutput>
{
}