namespace Oak.Lua;

/// <summary>
///     Lua 词法分析器 Token 类型。
/// </summary>
public enum LuaTokenType
{
    /// <summary>
    ///     名称标识符。
    /// </summary>
    Name,

    /// <summary>
    ///     整数。
    /// </summary>
    Integer,

    /// <summary>
    ///     浮点数。
    /// </summary>
    Float,

    /// <summary>
    ///     字符串。
    /// </summary>
    String,

    /// <summary>
    ///     长字符串（[[...]]）。
    /// </summary>
    LongString,

    /// <summary>
    ///     注释。
    /// </summary>
    Comment,

    /// <summary>
    ///     长注释（--[[...]]）。
    /// </summary>
    LongComment,

    /// <summary>
    ///     关键字 and。
    /// </summary>
    And,

    /// <summary>
    ///     关键字 break。
    /// </summary>
    Break,

    /// <summary>
    ///     关键字 do。
    /// </summary>
    Do,

    /// <summary>
    ///     关键字 else。
    /// </summary>
    Else,

    /// <summary>
    ///     关键字 elseif。
    /// </summary>
    ElseIf,

    /// <summary>
    ///     关键字 end。
    /// </summary>
    End,

    /// <summary>
    ///     关键字 false。
    /// </summary>
    False,

    /// <summary>
    ///     关键字 for。
    /// </summary>
    For,

    /// <summary>
    ///     关键字 function。
    /// </summary>
    Function,

    /// <summary>
    ///     关键字 goto。
    /// </summary>
    Goto,

    /// <summary>
    ///     关键字 if。
    /// </summary>
    If,

    /// <summary>
    ///     关键字 in。
    /// </summary>
    In,

    /// <summary>
    ///     关键字 local。
    /// </summary>
    Local,

    /// <summary>
    ///     关键字 nil。
    /// </summary>
    Nil,

    /// <summary>
    ///     关键字 not。
    /// </summary>
    Not,

    /// <summary>
    ///     关键字 or。
    /// </summary>
    Or,

    /// <summary>
    ///     关键字 repeat。
    /// </summary>
    Repeat,

    /// <summary>
    ///     关键字 return。
    /// </summary>
    Return,

    /// <summary>
    ///     关键字 then。
    /// </summary>
    Then,

    /// <summary>
    ///     关键字 true。
    /// </summary>
    True,

    /// <summary>
    ///     关键字 until。
    /// </summary>
    Until,

    /// <summary>
    ///     关键字 while。
    /// </summary>
    While,

    /// <summary>
    ///     加号。
    /// </summary>
    Plus,

    /// <summary>
    ///     减号。
    /// </summary>
    Minus,

    /// <summary>
    ///     星号。
    /// </summary>
    Star,

    /// <summary>
    ///     斜杠。
    /// </summary>
    Slash,

    /// <summary>
    ///     百分号。
    /// </summary>
    Percent,

    /// <summary>
    ///     脱字号。
    /// </summary>
    Caret,

    /// <summary>
    ///     井号。
    /// </summary>
    Hash,

    /// <summary>
    ///     等于。
    /// </summary>
    EqualEqual,

    /// <summary>
    ///     不等于。
    /// </summary>
    TildeEqual,

    /// <summary>
    ///     小于。
    /// </summary>
    Less,

    /// <summary>
    ///     小于等于。
    /// </summary>
    LessEqual,

    /// <summary>
    ///     大于。
    /// </summary>
    Greater,

    /// <summary>
    ///     大于等于。
    /// </summary>
    GreaterEqual,

    /// <summary>
    ///     赋值。
    /// </summary>
    Equal,

    /// <summary>
    ///     左括号。
    /// </summary>
    LeftParen,

    /// <summary>
    ///     右括号。
    /// </summary>
    RightParen,

    /// <summary>
    ///     左花括号。
    /// </summary>
    LeftBrace,

    /// <summary>
    ///     右花括号。
    /// </summary>
    RightBrace,

    /// <summary>
    ///     左方括号。
    /// </summary>
    LeftBracket,

    /// <summary>
    ///     右方括号。
    /// </summary>
    RightBracket,

    /// <summary>
    ///     双冒号。
    /// </summary>
    DoubleColon,

    /// <summary>
    ///     分号。
    /// </summary>
    Semicolon,

    /// <summary>
    ///     冒号。
    /// </summary>
    Colon,

    /// <summary>
    ///     逗号。
    /// </summary>
    Comma,

    /// <summary>
    ///     点。
    /// </summary>
    Dot,

    /// <summary>
    ///     连接点。
    /// </summary>
    Concat,

    /// <summary>
    ///     变长参数。
    /// </summary>
    Dots,

    /// <summary>
    ///     行尾。
    /// </summary>
    EndOfLine,

    /// <summary>
    ///     文件结尾。
    /// </summary>
    EndOfFile
}
