using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Interfaces
{
    /// <summary>
    /// Service for discovering source files in a directory structure.
    /// </summary>
    public interface ISourceFileDiscoveryService
    {
        /// <summary>
        /// Discovers and loads source files from the specified directory.
        /// </summary>
        /// <param name="directory">Root directory to search for source files</param>
        /// <returns>Collection of discovered source files</returns>
        Task<IEnumerable<SourceFile>> DiscoverSourceFilesAsync(string directory);

        /// <summary>
        /// Sets the file extensions to search for (e.g., ".cs", ".vb", ".fs")
        /// </summary>
        void SetFileExtensions(params string[] extensions);

        /// <summary>
        /// Sets directory names or path segments to exclude from search
        /// </summary>
        void SetExclusionFilters(params string[] filters);
    }
}
