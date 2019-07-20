namespace Oak.Valkyrie.Lexer;

/// <summary>
///     Valkyrie 词法节点类型
/// </summary>
public enum ValkyrieTokenKind : short
{
    #region Basics

    /// <summary>未知类型</summary>
    Error = -1,

    /// <summary>结束流，终止符</summary>
    Eos = 0,

    /// <summary>#</summary>
    CommentStart = 1,

    /// <summary>CommentContent</summary>
    CommentContent = 2,

    #endregion

    #region Keywords

    /// <summary>namespace</summary>
    Namespace = 27,

    /// <summary>using</summary>
    Using = 28,

    /// <summary>let</summary>
    Let = 10,

    /// <summary>micro</summary>
    Micro = 11,

    /// <summary>mezzo</summary>
    Mezzo = 12,

    /// <summary>macro</summary>
    Macro = 13,

    /// <summary>if</summary>
    If = 19,

    /// <summary>loop</summary>
    Loop = 21,

    /// <summary>while</summary>
    While = 22,

    /// <summary>until</summary>
    Until = 23,

    /// <summary>structure</summary>
    Structure = 123,

    /// <summary>class</summary>
    Class = 29,

    /// <summary>enums</summary>
    Enums = 30,

    /// <summary>flags</summary>
    Flags = 31,

    /// <summary>union</summary>
    Union = 32,

    /// <summary>unite</summary>
    Unite = 39,

    /// <summary>trait</summary>
    Trait = 48,

    /// <summary>match</summary>
    Match = 24,

    /// <summary>catch</summary>
    Catch = 38,

    /// <summary>case</summary>
    Case = 25,

    /// <summary>type</summary>
    Type = 40,

    /// <summary>when</summary>
    When = 66,

    /// <summary>else</summary>
    Else = 20,

    /// <summary>end</summary>
    End = 26,

    /// <summary>break</summary>
    Break = 35,

    /// <summary>continue</summary>
    Continue = 36,

    /// <summary>return</summary>
    Return = 18,

    /// <summary>resume</summary>
    Resume = 37,


    /// <summary>in</summary>
    In = 34,

    /// <summary>is</summary>
    Is = 67,

    /// <summary>as</summary>
    As = 68,

    /// <summary>where</summary>
    Where = 41,

    #endregion

    #region Literals

    /// <summary>null</summary>
    Null = 82,

    /// <summary>true</summary>
    True = 80,

    /// <summary>false</summary>
    False = 81,

    /// <summary>标识符</summary>
    Identifier = 84,

    /// <summary>数字</summary>
    Number = 85,

    /// <summary>字符串</summary>
    String = 86,

    #endregion

    #region ECS Extension

    /// <summary>component</summary>
    Component = 1001,

    /// <summary>system</summary>
    System = 1002,

    #endregion

    #region Widget Extension

    /// <summary>widget</summary>
    Widget = 1401,

    #endregion

    #region Schema Extension

    /// <summary>model</summary>
    Model = 1201,

    /// <summary>service</summary>
    Service = 1202,

    /// <summary>model</summary>
    Message = 1203,

    #endregion

    #region Shader Extension

    /// <summary>shader</summary>
    Shader = 1501,

    /// <summary>vertex</summary>
    Vertex = 52,

    /// <summary>fragment</summary>
    Fragment = 53,

    /// <summary>compute</summary>
    Compute = 54,

    /// <summary>uniform</summary>
    Uniform = 55,

    /// <summary>varying</summary>
    Varying = 56,

    /// <summary>cbuffer</summary>
    CBuffer = 57,

    /// <summary>texture</summary>
    Texture = 58,

    /// <summary>sampler</summary>
    Sampler = 59,

    /// <summary>discard</summary>
    Discard = 60,

    /// <summary>raygen</summary>
    Raygen = 61,

    /// <summary>closesthit</summary>
    Closesthit = 62,

    /// <summary>anyhit</summary>
    Anyhit = 63,

    /// <summary>miss</summary>
    Miss = 64,

    /// <summary>constant</summary>
    Constant = 65,

    /// <summary>binding</summary>
    Binding = 66,

    #endregion

    #region Neural Extension

    /// <summary>neural</summary>
    Neural = 49,

    #endregion

    #region Operators

    /// <summary>+</summary>
    Plus = 100,

    /// <summary>-</summary>
    Minus = 101,

    /// <summary>*</summary>
    Star = 102,

    /// <summary>/</summary>
    Slash = 103,

    /// <summary>%</summary>
    Percent = 104,

    /// <summary>=</summary>
    Equal = 105,

    /// <summary>+=</summary>
    PlusEqual = 106,

    /// <summary>-=</summary>
    MinusEqual = 107,

    /// <summary>*=</summary>
    StarEqual = 108,

    /// <summary>/=</summary>
    SlashEqual = 109,

    /// <summary>%=</summary>
    PercentEqual = 110,

    /// <summary>==</summary>
    EqualEqual = 111,

    /// <summary>!=</summary>
    BangEqual = 112,

    /// <summary>&lt;=</summary>
    LessEqual = 115,

    /// <summary>&gt;=</summary>
    GreaterEqual = 116,

    /// <summary>&amp;&amp;</summary>
    AmpAmp = 117,

    /// <summary>||</summary>
    PipePipe = 118,

    /// <summary>!</summary>
    Bang = 119,

    /// <summary>&amp;</summary>
    Amp = 120,

    /// <summary>|</summary>
    Pipe = 121,

    /// <summary>^</summary>
    Power = 122,

    /// <summary>~</summary>
    Tilde = 123,

    /// <summary>&amp;=</summary>
    AmpEqual = 124,

    /// <summary>|=</summary>
    PipeEqual = 125,

    /// <summary>^=</summary>
    CaretEqual = 126,

    /// <summary>&lt;&lt;</summary>
    LessLess = 127,

    /// <summary>&gt;&gt;</summary>
    GreaterGreater = 128,

    /// <summary>&lt;&lt;=</summary>
    LessLessEqual = 129,

    /// <summary>&gt;&gt;=</summary>
    GreaterGreaterEqual = 130,

    /// <summary>-&gt;</summary>
    Arrow = 131,

    /// <summary>=&gt;</summary>
    FatArrow = 132,

    /// <summary>??</summary>
    QuestionQuestion = 133,

    /// <summary>++</summary>
    PlusPlus = 134,

    /// <summary>--</summary>
    MinusMinus = 135,

    /// <summary>?</summary>
    Question = 136,

    /// <summary>.</summary>
    Dot = 137,

    #endregion

    #region Punctuations

    /// <summary>:</summary>
    Colon = 200,

    /// <summary>::</summary>
    DoubleColon = 201,

    /// <summary>,</summary>
    Comma = 202,

    /// <summary>;</summary>
    Semicolon = 203,

    #endregion

    #region Delimiters

    /// <summary>&lt;#</summary>
    CommentL = 401,

    /// <summary>#&gt;</summary>
    CommentR = 402,

    /// <summary>(</summary>
    ParenthesisL = 431,

    /// <summary>)</summary>
    ParenthesisR = 432,

    /// <summary>[</summary>
    BracketL = 403,

    /// <summary>]</summary>
    BracketR = 404,

    /// <summary>⁅</summary>
    OffsetL = 405,

    /// <summary>⁆</summary>
    OffsetR = 406,

    /// <summary>{</summary>
    BraceL = 207,

    /// <summary>}</summary>
    BraceR = 208,

    /// <summary>&lt;</summary>
    Less = 209,

    /// <summary>&gt;</summary>
    Greater = 210,

    /// <summary>⟨</summary>
    GenericL = 211,

    /// <summary>⟩</summary>
    GenericR = 212,

    /// <summary>&lt;%</summary>
    TemplateL = 213,

    /// <summary>%&gt;</summary>
    TemplateR = 214,

    #endregion
}
