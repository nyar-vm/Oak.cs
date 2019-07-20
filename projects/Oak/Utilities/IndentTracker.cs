namespace Oak.Utilities;

/// <summary>
///     缩进追踪器，为缩进敏感语言提供 INDENT/DEDENT 逻辑
/// </summary>
public sealed class IndentTracker
{
    private readonly Stack<int> _indentStack = new();

    /// <summary>
    ///     初始化缩进追踪器，初始缩进级别为 0
    /// </summary>
    public IndentTracker()
    {
        _indentStack.Push(0);
    }

    /// <summary>
    ///     当前缩进级别栈
    /// </summary>
    public IReadOnlyList<int> IndentLevels => _indentStack.ToArray();

    /// <summary>
    ///     当前缩进级别
    /// </summary>
    public int CurrentIndent => _indentStack.Count > 0 ? _indentStack.Peek() : 0;

    /// <summary>
    ///     处理一行的缩进，返回产生的缩进事件列表
    /// </summary>
    /// <param name="indentLevel">当前行的缩进级别（空格数或制表符等效数）</param>
    /// <returns>缩进事件列表</returns>
    public IReadOnlyList<IndentEvent> ProcessLine(int indentLevel)
    {
        var events = new List<IndentEvent>();
        var current = _indentStack.Peek();

        if (indentLevel > current)
        {
            _indentStack.Push(indentLevel);
            events.Add(IndentEvent.Indent);
        }
        else if (indentLevel < current)
        {
            while (_indentStack.Count > 1 && _indentStack.Peek() > indentLevel)
            {
                _indentStack.Pop();
                events.Add(IndentEvent.Dedent);
            }

            if (_indentStack.Peek() != indentLevel)
            {
                _indentStack.Push(indentLevel);
                events.Add(IndentEvent.Indent);
            }
        }

        return events;
    }

    /// <summary>
    ///     重置追踪器到初始状态
    /// </summary>
    public void Reset()
    {
        _indentStack.Clear();
        _indentStack.Push(0);
    }

    /// <summary>
    ///     产生文件末尾所需的剩余 DEDENT 事件
    /// </summary>
    /// <returns>DEDENT 事件列表</returns>
    public IReadOnlyList<IndentEvent> Close()
    {
        var events = new List<IndentEvent>();
        while (_indentStack.Count > 1)
        {
            _indentStack.Pop();
            events.Add(IndentEvent.Dedent);
        }

        return events;
    }
}

/// <summary>
///     缩进事件类型
/// </summary>
public enum IndentEvent
{
    /// <summary>
    ///     缩进增加
    /// </summary>
    Indent,

    /// <summary>
    ///     缩进减少
    /// </summary>
    Dedent
}