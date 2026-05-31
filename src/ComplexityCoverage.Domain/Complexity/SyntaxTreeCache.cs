using Microsoft.CodeAnalysis.CSharp;
using ComplexityCoverage.Domain.Interfaces;
using System.Collections.Concurrent;

namespace ComplexityCoverage.Domain.Complexity
{
    /// <summary>
    /// Default implementation of ISyntaxTreeCache using a thread-safe dictionary for caching.
    /// </summary>
    public class SyntaxTreeCache : ISyntaxTreeCache
    {
        private readonly ConcurrentDictionary<string, CachedSyntaxTreeData> _cache = new();

        /// <summary>
        /// Gets or creates a cached syntax tree for the given code content.
        /// Uses the content as the cache key, so identical code is only parsed once.
        /// </summary>
        public object GetOrCreateSyntaxTree(string content)
        {
            return _cache.GetOrAdd(content, static key =>
            {
                var tree = CSharpSyntaxTree.ParseText(key);
                var root = tree.GetRoot();
                return new CachedSyntaxTreeData(tree, root);
            });
        }

        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
    }
}
