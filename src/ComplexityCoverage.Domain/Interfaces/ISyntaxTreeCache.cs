namespace ComplexityCoverage.Domain.Interfaces
{
    /// <summary>
    /// Caches parsed syntax trees to avoid re-parsing the same code multiple times.
    /// This is important for performance when calculating complexity weights for multiple lines.
    /// </summary>
    public interface ISyntaxTreeCache
    {
        /// <summary>
        /// Gets or creates a cached syntax tree for the given code content.
        /// </summary>
        /// <param name="content">The C# source code to parse</param>
        /// <returns>The cached syntax tree data (implementation-specific)</returns>
        object GetOrCreateSyntaxTree(string content);

        /// <summary>
        /// Clears all cached entries. Call this when memory is needed or analysis is complete.
        /// </summary>
        void Clear();
    }
}
