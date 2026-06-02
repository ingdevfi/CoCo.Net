using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ComplexityCoverage.Domain.Interfaces;
using System.Collections.Concurrent;

namespace ComplexityCoverage.Domain.Complexity
{
    /// <summary>
    /// Custom syntax tree cache that wraps code in a class if no methods are found.
    /// This is used by McCabeComplexityStrategy to handle test code snippets that don't have containing classes.
    /// </summary>
    internal class WrappingSyntaxTreeCache : ISyntaxTreeCache
    {
        private readonly ConcurrentDictionary<string, object> _cache = new();

        public object GetOrCreateSyntaxTree(string content)
        {
            return _cache.GetOrAdd(content, static key =>
            {
                // Try to parse as-is
                var tree = CSharpSyntaxTree.ParseText(key);
                var root = (CompilationUnitSyntax)tree.GetRoot();
                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

                // If no methods found, wrap in a class
                if (methods.Count == 0)
                {
                    var wrappedCode = $"public class TestClass {{\n{key}\n}}";
                    tree = CSharpSyntaxTree.ParseText(wrappedCode);
                    root = (CompilationUnitSyntax)tree.GetRoot();
                }

                return new CachedSyntaxTreeData(tree, root);
            });
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
