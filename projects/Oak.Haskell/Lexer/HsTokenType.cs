namespace Oak.Haskell.Lexer;

public enum HsTokenType
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
    Qualified
}
