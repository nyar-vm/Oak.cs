using Oak.Glsl;
using Oak.Hlsl;
using Oak.Jasmin;
using Oak.Javap;
using Oak.Msil;
using Oak.Wat;

namespace Oak.ShaderLowLevel.Tests;

public class GlslLexerTests
{
    private readonly GlslLexer _lexer = new();

    [Fact]
    public void Tokenize_VersionDirective_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("#version 450");
        Assert.NotEmpty(tokens);
        Assert.Equal(GlslTokenType.Preprocessor, tokens[0].Type);
        Assert.Equal("#version 450", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_TypeKeywords_ShouldReturnKeywordTokens()
    {
        var tokens = _lexer.Tokenize("void float vec2 vec3 vec4 mat4");
        Assert.Equal(6, tokens.Count(t => t.Type != GlslTokenType.EndOfFile));
        Assert.Equal(GlslTokenType.Void, tokens[0].Type);
        Assert.Equal(GlslTokenType.Float, tokens[1].Type);
        Assert.Equal(GlslTokenType.Vec, tokens[2].Type);
        Assert.Equal(GlslTokenType.Vec, tokens[3].Type);
        Assert.Equal(GlslTokenType.Vec, tokens[4].Type);
        Assert.Equal(GlslTokenType.Mat, tokens[5].Type);
    }

    [Fact]
    public void Tokenize_Qualifiers_ShouldReturnKeywordTokens()
    {
        var tokens = _lexer.Tokenize("uniform in out layout");
        Assert.Equal(4, tokens.Count(t => t.Type != GlslTokenType.EndOfFile));
        Assert.Equal(GlslTokenType.Uniform, tokens[0].Type);
        Assert.Equal(GlslTokenType.In, tokens[1].Type);
        Assert.Equal(GlslTokenType.Out, tokens[2].Type);
        Assert.Equal(GlslTokenType.Layout, tokens[3].Type);
    }

    [Fact]
    public void Tokenize_FunctionDecl_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("void main() { }");
        Assert.Equal(GlslTokenType.Void, tokens[0].Type);
        Assert.Equal(GlslTokenType.Identifier, tokens[1].Type);
        Assert.Equal("main", tokens[1].Text);
        Assert.Equal(GlslTokenType.LeftParen, tokens[2].Type);
        Assert.Equal(GlslTokenType.RightParen, tokens[3].Type);
        Assert.Equal(GlslTokenType.LeftBrace, tokens[4].Type);
        Assert.Equal(GlslTokenType.RightBrace, tokens[5].Type);
    }

    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = _lexer.Tokenize("");
        Assert.Single(tokens);
        Assert.Equal(GlslTokenType.EndOfFile, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_LogicalAnd_ShouldReturnLogicalAndToken()
    {
        var tokens = _lexer.Tokenize("a && b");
        var andToken = tokens.FirstOrDefault(t => t.Type == GlslTokenType.LogicalAnd);
        Assert.NotNull(andToken);
        Assert.Equal("&&", andToken.Text);
    }

    [Fact]
    public void Tokenize_LogicalOr_ShouldReturnLogicalOrToken()
    {
        var tokens = _lexer.Tokenize("a || b");
        var orToken = tokens.FirstOrDefault(t => t.Type == GlslTokenType.LogicalOr);
        Assert.NotNull(orToken);
        Assert.Equal("||", orToken.Text);
    }

    [Fact]
    public void Tokenize_LogicalOperatorsNotSplitIntoSingleAmpersandOrPipe()
    {
        var tokens = _lexer.Tokenize("&& ||");
        Assert.Equal(3, tokens.Count(t => t.Type != GlslTokenType.EndOfFile));
        Assert.Equal(GlslTokenType.LogicalAnd, tokens[0].Type);
        Assert.Equal(GlslTokenType.LogicalOr, tokens[1].Type);
        Assert.DoesNotContain(tokens, t => t.Type == GlslTokenType.Ampersand);
        Assert.DoesNotContain(tokens, t => t.Type == GlslTokenType.Pipe);
    }

    [Fact]
    public void Tokenize_CentroidKeyword_ShouldReturnCentroidToken()
    {
        var tokens = _lexer.Tokenize("centroid out vec4 color");
        Assert.Equal(GlslTokenType.Centroid, tokens[0].Type);
        Assert.Equal("centroid", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_IntegerSamplerTypes_ShouldReturnISamplerToken()
    {
        var tokens = _lexer.Tokenize("isampler2D isampler3D isamplerCube");
        Assert.Equal(3, tokens.Count(t => t.Type == GlslTokenType.ISampler));
        Assert.Equal("isampler2D", tokens[0].Text);
        Assert.Equal("isampler3D", tokens[1].Text);
        Assert.Equal("isamplerCube", tokens[2].Text);
    }

    [Fact]
    public void Tokenize_UnsignedSamplerTypes_ShouldReturnUSamplerToken()
    {
        var tokens = _lexer.Tokenize("usampler2D usampler3D usamplerCube");
        Assert.Equal(3, tokens.Count(t => t.Type == GlslTokenType.USampler));
        Assert.Equal("usampler2D", tokens[0].Text);
        Assert.Equal("usampler3D", tokens[1].Text);
        Assert.Equal("usamplerCube", tokens[2].Text);
    }

    [Fact]
    public void Tokenize_SamplerTypes_ShouldReturnSamplerToken()
    {
        var tokens = _lexer.Tokenize("sampler2D samplerCube sampler2DShadow");
        Assert.Equal(3, tokens.Count(t => t.Type == GlslTokenType.Sampler));
    }

    [Fact]
    public void Tokenize_InterpolationQualifiers_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("flat smooth noperspective centroid");
        Assert.Equal(GlslTokenType.Flat, tokens[0].Type);
        Assert.Equal(GlslTokenType.Smooth, tokens[1].Type);
        Assert.Equal(GlslTokenType.Noperspective, tokens[2].Type);
        Assert.Equal(GlslTokenType.Centroid, tokens[3].Type);
    }

    [Fact]
    public void Tokenize_ComparisonOperators_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("== != < > <= >=");
        Assert.Equal(6, tokens.Count(t => t.Type != GlslTokenType.EndOfFile));
        Assert.Equal(GlslTokenType.EqualEqual, tokens[0].Type);
        Assert.Equal(GlslTokenType.NotEqual, tokens[1].Type);
        Assert.Equal(GlslTokenType.Less, tokens[2].Type);
        Assert.Equal(GlslTokenType.Greater, tokens[3].Type);
        Assert.Equal(GlslTokenType.LessEqual, tokens[4].Type);
        Assert.Equal(GlslTokenType.GreaterEqual, tokens[5].Type);
    }

    [Fact]
    public void Tokenize_BitwiseOperators_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("& | ^ ~");
        Assert.Equal(4, tokens.Count(t => t.Type != GlslTokenType.EndOfFile));
        Assert.Equal(GlslTokenType.Ampersand, tokens[0].Type);
        Assert.Equal(GlslTokenType.Pipe, tokens[1].Type);
        Assert.Equal(GlslTokenType.Caret, tokens[2].Type);
        Assert.Equal(GlslTokenType.Tilde, tokens[3].Type);
    }

    [Fact]
    public void Tokenize_ShiftOperators_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("<< >> <<= >>=");
        Assert.Equal(4, tokens.Count(t => t.Type != GlslTokenType.EndOfFile));
        Assert.Equal(GlslTokenType.LeftShift, tokens[0].Type);
        Assert.Equal(GlslTokenType.RightShift, tokens[1].Type);
        Assert.Equal(GlslTokenType.LeftShiftEqual, tokens[2].Type);
        Assert.Equal(GlslTokenType.RightShiftEqual, tokens[3].Type);
    }

    [Fact]
    public void Tokenize_NumberLiterals_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("42 3.14 1.0e10 0xFF 1u 2.0f 3L");
        Assert.Equal(GlslTokenType.IntConstant, tokens[0].Type);
        Assert.Equal(GlslTokenType.FloatConstant, tokens[1].Type);
        Assert.Equal(GlslTokenType.FloatConstant, tokens[2].Type);
        Assert.Equal(GlslTokenType.IntConstant, tokens[3].Type);
        Assert.Equal(GlslTokenType.UintConstant, tokens[4].Type);
        Assert.Equal(GlslTokenType.FloatConstant, tokens[5].Type);
        Assert.Equal(GlslTokenType.DoubleConstant, tokens[6].Type);
    }

    [Fact]
    public void Tokenize_CommentTypes_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("// line comment\n/* block comment */");
        Assert.Equal(2, tokens.Count(t => t.Type is GlslTokenType.LineComment or GlslTokenType.BlockComment));
        Assert.Equal(GlslTokenType.LineComment, tokens[0].Type);
        Assert.Equal(GlslTokenType.BlockComment, tokens[1].Type);
    }
}

public class HlslLexerTests
{
    private readonly HlslLexer _lexer = new();

    [Fact]
    public void Tokenize_TypeKeywords_ShouldReturnKeywordTokens()
    {
        var tokens = _lexer.Tokenize("float4 float3 float2 cbuffer Texture2D");
        Assert.Equal(5, tokens.Count(t => t.Type != HlslTokenType.EndOfFile));
        Assert.Equal(HlslTokenType.FloatType, tokens[0].Type);
        Assert.Equal(HlslTokenType.FloatType, tokens[1].Type);
        Assert.Equal(HlslTokenType.FloatType, tokens[2].Type);
        Assert.Equal(HlslTokenType.CBuffer, tokens[3].Type);
        Assert.Equal(HlslTokenType.Texture2D, tokens[4].Type);
    }

    [Fact]
    public void Tokenize_Semantics_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("SV_POSITION SV_TARGET");
        Assert.Equal(2, tokens.Count(t => t.Type != HlslTokenType.EndOfFile));
    }

    [Fact]
    public void Tokenize_FunctionDecl_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("float4 main() { }");
        Assert.Equal(HlslTokenType.FloatType, tokens[0].Type);
        Assert.Equal(HlslTokenType.Identifier, tokens[1].Type);
        Assert.Equal("main", tokens[1].Text);
    }

    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = _lexer.Tokenize("");
        Assert.Single(tokens);
        Assert.Equal(HlslTokenType.EndOfFile, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_LogicalAnd_ShouldReturnLogicalAndToken()
    {
        var tokens = _lexer.Tokenize("a && b");
        var andToken = tokens.FirstOrDefault(t => t.Type == HlslTokenType.LogicalAnd);
        Assert.NotNull(andToken);
        Assert.Equal("&&", andToken.Text);
    }

    [Fact]
    public void Tokenize_LogicalOr_ShouldReturnLogicalOrToken()
    {
        var tokens = _lexer.Tokenize("a || b");
        var orToken = tokens.FirstOrDefault(t => t.Type == HlslTokenType.LogicalOr);
        Assert.NotNull(orToken);
        Assert.Equal("||", orToken.Text);
    }

    [Fact]
    public void Tokenize_LogicalOperatorsNotSplitIntoSingleAmpersandOrPipe()
    {
        var tokens = _lexer.Tokenize("&& ||");
        Assert.Equal(3, tokens.Count(t => t.Type != HlslTokenType.EndOfFile));
        Assert.Equal(HlslTokenType.LogicalAnd, tokens[0].Type);
        Assert.Equal(HlslTokenType.LogicalOr, tokens[1].Type);
        Assert.DoesNotContain(tokens, t => t.Type == HlslTokenType.Ampersand);
        Assert.DoesNotContain(tokens, t => t.Type == HlslTokenType.Pipe);
    }

    [Fact]
    public void Tokenize_CentroidKeyword_ShouldReturnCentroidToken()
    {
        var tokens = _lexer.Tokenize("centroid float4 color");
        Assert.Equal(HlslTokenType.Centroid, tokens[0].Type);
        Assert.Equal("centroid", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_TextureTypes_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("Texture2D Texture3D TextureCube Texture2DArray");
        Assert.Equal(HlslTokenType.Texture2D, tokens[0].Type);
        Assert.Equal(HlslTokenType.Texture3D, tokens[1].Type);
        Assert.Equal(HlslTokenType.TextureCube, tokens[2].Type);
        Assert.Equal(HlslTokenType.Texture2DArray, tokens[3].Type);
    }

    [Fact]
    public void Tokenize_RWBufferTypes_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("RWTexture2D RWBuffer RWByteAddressBuffer");
        Assert.Equal(HlslTokenType.RWTexture2D, tokens[0].Type);
        Assert.Equal(HlslTokenType.RWBuffer, tokens[1].Type);
        Assert.Equal(HlslTokenType.RWByteAddressBuffer, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_ComparisonOperators_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("== != < > <= >=");
        Assert.Equal(6, tokens.Count(t => t.Type != HlslTokenType.EndOfFile));
        Assert.Equal(HlslTokenType.EqualEqual, tokens[0].Type);
        Assert.Equal(HlslTokenType.NotEqual, tokens[1].Type);
        Assert.Equal(HlslTokenType.Less, tokens[2].Type);
        Assert.Equal(HlslTokenType.Greater, tokens[3].Type);
        Assert.Equal(HlslTokenType.LessEqual, tokens[4].Type);
        Assert.Equal(HlslTokenType.GreaterEqual, tokens[5].Type);
    }

    [Fact]
    public void Tokenize_NumberLiterals_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("42 3.14 1.0e10 0xFF 1u 2.0f 3L 1h");
        Assert.Equal(HlslTokenType.IntLiteral, tokens[0].Type);
        Assert.Equal(HlslTokenType.FloatLiteral, tokens[1].Type);
        Assert.Equal(HlslTokenType.FloatLiteral, tokens[2].Type);
        Assert.Equal(HlslTokenType.IntLiteral, tokens[3].Type);
        Assert.Equal(HlslTokenType.UintLiteral, tokens[4].Type);
        Assert.Equal(HlslTokenType.FloatLiteral, tokens[5].Type);
        Assert.Equal(HlslTokenType.DoubleLiteral, tokens[6].Type);
        Assert.Equal(HlslTokenType.FloatLiteral, tokens[7].Type);
    }

    [Fact]
    public void Tokenize_InterpolationQualifiers_ShouldReturnCorrectTokens()
    {
        var tokens = _lexer.Tokenize("nointerpolation noperspective linear centroid");
        Assert.Equal(HlslTokenType.Nointerpolation, tokens[0].Type);
        Assert.Equal(HlslTokenType.Noperspective, tokens[1].Type);
        Assert.Equal(HlslTokenType.Linear, tokens[2].Type);
        Assert.Equal(HlslTokenType.Centroid, tokens[3].Type);
    }
}

public class WatLexerTests
{
    private readonly WatLexer _lexer = new();

    [Fact]
    public void Tokenize_ModuleDecl_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("(module)");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_FunctionDecl_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("(func (export \"main\"))");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_Instructions_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("i32.add i32.const 42");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = _lexer.Tokenize("");
        Assert.NotEmpty(tokens);
    }
}

public class JmLexerTests
{
    private readonly JmLexer _lexer = new();

    [Fact]
    public void Tokenize_ClassDirective_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize(".class public Hello");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_MethodDirective_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize(".method public static void main()");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_Instructions_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("getstatic invokevirtual return");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = _lexer.Tokenize("");
        Assert.NotEmpty(tokens);
    }
}

public class JvpLexerTests
{
    private readonly JvpLexer _lexer = new();

    [Fact]
    public void Tokenize_ClassHeader_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("public class Hello extends Object");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_MethodHeader_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("public static void main(java.lang.String[])");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = _lexer.Tokenize("");
        Assert.NotEmpty(tokens);
    }
}

public class MsilLexerTests
{
    private readonly MsilLexer _lexer = new();

    [Fact]
    public void Tokenize_MethodDecl_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize(".method public static void Main()");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_Instructions_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("ldstr \"Hello\" call void WriteLine");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = _lexer.Tokenize("");
        Assert.NotEmpty(tokens);
    }
}
