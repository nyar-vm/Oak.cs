namespace Oak.Scss;

/// <summary>
///     SCSS Mixin 定义
/// </summary>
public sealed class ScssMixin
{
    public ScssMixin(string name, IReadOnlyList<string> parameters, string body)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
    }

    /// <summary>
    ///     Mixin 名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     参数列表
    /// </summary>
    public IReadOnlyList<string> Parameters { get; }

    /// <summary>
    ///     Mixin 体
    /// </summary>
    public string Body { get; }
}