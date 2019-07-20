using Oak.Syntax;

namespace Oak.Valkyrie.Lexer;

public static class ValkyrieNodeKindExtensions
{
    public static NodeKind ToNodeKind(this ValkyrieTokenKind kind) => new((int)kind);

    /// <param name="kind"></param>
    extension(NodeKind kind)
    {
        /// <summary>
        /// 约定 100 到 1000 为关键词区
        /// </summary>
        /// <returns></returns>
        public bool IsKeyword()
        {
            var vKind = (ValkyrieTokenKind)kind.Value;
            return vKind is >= ValkyrieTokenKind.Let and <= ValkyrieTokenKind.As
                or ValkyrieTokenKind.Component
                or ValkyrieTokenKind.System
                or ValkyrieTokenKind.Widget
                or ValkyrieTokenKind.Model
                or ValkyrieTokenKind.Service
                or ValkyrieTokenKind.Shader
                or ValkyrieTokenKind.Uniform
                or ValkyrieTokenKind.Varying
                or ValkyrieTokenKind.CBuffer
                or ValkyrieTokenKind.Texture
                or ValkyrieTokenKind.Sampler
                or ValkyrieTokenKind.Discard;
        }

        /// <summary>
        /// 约定 2000 以上为操作符区
        /// </summary>
        /// <returns></returns>
        public bool IsOperator()
        {
            var vKind = (ValkyrieTokenKind)kind.Value;
            return vKind is >= ValkyrieTokenKind.Plus and <= ValkyrieTokenKind.Dot;
        }

        public bool IsLiteral()
        {
            var vKind = (ValkyrieTokenKind)kind.Value;
            return vKind is >= ValkyrieTokenKind.True and <= ValkyrieTokenKind.Null;
        }
    }
}
