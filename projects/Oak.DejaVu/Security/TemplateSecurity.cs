using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace Oak.DejaVu.Security;

/// <summary>
///     模板安全设置
/// </summary>
public sealed class TemplateSecurityOptions
{
    /// <summary>
    ///     是否启用沙箱模式
    /// </summary>
    public bool EnableSandbox { get; init; } = true;

    /// <summary>
    ///     允许访问的类型列表
    /// </summary>
    public IReadOnlyList<Type> AllowedTypes { get; init; } = new List<Type>();

    /// <summary>
    ///     禁止访问的类型列表
    /// </summary>
    public IReadOnlyList<Type> BlockedTypes { get; init; } = new List<Type>
    {
        typeof(File),
        typeof(FileStream),
        typeof(Directory),
        typeof(WebClient),
        typeof(HttpClient),
        typeof(Process)
    };

    /// <summary>
    ///     允许调用的方法列表
    /// </summary>
    public IReadOnlyList<string> AllowedMethods { get; init; } = new List<string>();

    /// <summary>
    ///     禁止调用的方法列表
    /// </summary>
    public IReadOnlyList<string> BlockedMethods { get; init; } = new List<string>
    {
        "GetType",
        "GetProperties",
        "GetFields",
        "GetMethods",
        "Invoke",
        "CreateInstance"
    };

    /// <summary>
    ///     最大循环迭代次数
    /// </summary>
    public int MaxLoopIterations { get; init; } = 1000;

    /// <summary>
    ///     最大模板渲染时间（毫秒）
    /// </summary>
    public int MaxRenderTime { get; init; } = 5000;
}

/// <summary>
///     模板安全验证器
/// </summary>
public sealed class TemplateSecurityValidator
{
    private readonly TemplateSecurityOptions _options;

    public TemplateSecurityValidator(TemplateSecurityOptions? options = null)
    {
        _options = options ?? new TemplateSecurityOptions();
    }

    /// <summary>
    ///     验证类型访问
    /// </summary>
    public bool ValidateTypeAccess(Type type)
    {
        if (!_options.EnableSandbox)
            return true;

        if (_options.BlockedTypes.Contains(type))
            return false;

        if (_options.AllowedTypes.Count > 0 && !_options.AllowedTypes.Contains(type))
            return false;

        return true;
    }

    /// <summary>
    ///     验证方法调用
    /// </summary>
    public bool ValidateMethodCall(MethodInfo method)
    {
        if (!_options.EnableSandbox)
            return true;

        var methodName = method.Name;

        if (_options.BlockedMethods.Contains(methodName))
            return false;

        if (_options.AllowedMethods.Count > 0 && !_options.AllowedMethods.Contains(methodName))
            return false;

        return true;
    }

    /// <summary>
    ///     验证循环迭代次数
    /// </summary>
    public bool ValidateLoopIteration(int currentIteration)
    {
        if (!_options.EnableSandbox)
            return true;

        return currentIteration < _options.MaxLoopIterations;
    }
}

/// <summary>
///     模板渲染异常
/// </summary>
public class TemplateRenderException : Exception
{
    public TemplateRenderException(string message) : base(message)
    {
    }

    public TemplateRenderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
///     模板安全异常
/// </summary>
public class TemplateSecurityException : TemplateRenderException
{
    public TemplateSecurityException(string message) : base(message)
    {
    }
}

/// <summary>
///     模板超时异常
/// </summary>
public class TemplateTimeoutException : TemplateRenderException
{
    public TemplateTimeoutException(string message) : base(message)
    {
    }
}