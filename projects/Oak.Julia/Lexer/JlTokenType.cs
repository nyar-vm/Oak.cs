namespace Oak.Julia.Lexer;

public enum JlTokenType
{
    Eof,
    Identifier,
    Keyword,
    Number,
    String,
    Char,
    Operator,
    Delimiter,
    Punctuation,
    Comment,
    MacroName,
    CommandType,
    Symbol
}
