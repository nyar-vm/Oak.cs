using Oak.Diagnostics;

namespace Oak.Testing;

public abstract class TestBase
{
    protected virtual int TimeoutMs => 1000;

    protected T ExecuteWithTimeout<T>(Func<T> action, string operationName)
    {
        T? result = default;
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };

        thread.Start();

        if (!thread.Join(TimeoutMs))
        {
            throw new TimeoutException($"{operationName} 在 {TimeoutMs}ms 内未完成，可能存在死循环");
        }

        if (exception is not null) throw exception;

        return result!;
    }

    protected void ExecuteWithTimeout(Action action, string operationName)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };

        thread.Start();

        if (!thread.Join(TimeoutMs))
        {
            throw new TimeoutException($"{operationName} 在 {TimeoutMs}ms 内未完成，可能存在死循环");
        }

        if (exception is not null) throw exception;
    }
}
