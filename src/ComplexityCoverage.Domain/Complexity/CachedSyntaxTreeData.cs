using Microsoft.CodeAnalysis;

namespace ComplexityCoverage.Domain.Complexity
{
    /// <summary>
    /// Wrapper for cached syntax tree data.
    /// </summary>
    internal class CachedSyntaxTreeData(SyntaxTree tree, SyntaxNode root)
    {
        public SyntaxTree Tree { get; } = tree;
        public SyntaxNode Root { get; } = root;
    }
}
