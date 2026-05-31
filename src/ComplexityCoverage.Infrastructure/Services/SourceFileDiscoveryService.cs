using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Infrastructure.Services
{
    /// <summary>
    /// Default implementation of ISourceFileDiscoveryService.
    /// Discovers source files recursively from a directory with configurable filters.
    /// </summary>
    public class SourceFileDiscoveryService : ISourceFileDiscoveryService
    {
        private string[] _extensions = [".cs", ".vb", ".fs"];
        private string[] _exclusionFilters = ["bin", "obj", "Tests", ".git"];

        /// <summary>
        /// Sets the file extensions to search for (e.g., ".cs", ".vb", ".fs")
        /// </summary>
        public void SetFileExtensions(params string[] extensions)
        {
            _extensions = extensions.Length > 0 ? extensions : _extensions;
        }

        /// <summary>
        /// Sets directory names or path segments to exclude from search
        /// </summary>
        public void SetExclusionFilters(params string[] filters)
        {
            _exclusionFilters = filters.Length > 0 ? filters : _exclusionFilters;
        }

        /// <summary>
        /// Discovers and loads source files from the specified directory.
        /// </summary>
        public async Task<IEnumerable<SourceFile>> DiscoverSourceFilesAsync(string directory)
        {
            var extensionSet = new HashSet<string>(_extensions, StringComparer.OrdinalIgnoreCase);
            var filePaths = new List<string>();

            try
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
                {
                    if (!extensionSet.Contains(Path.GetExtension(file)))
                        continue;
                    if (IsExcluded(directory, file))
                        continue;
                    filePaths.Add(file);
                }
            }
            catch (IOException ex)
            {
                await Console.Error.WriteLineAsync($"[Warning] Cannot enumerate directory {directory}: {ex.Message}");
            }
            catch (UnauthorizedAccessException)
            {
                await Console.Error.WriteLineAsync($"[Warning] Access denied to directory {directory}");
            }

            // Read files concurrently but bounded to avoid exhausting the thread pool / file handles
            // on very large repos (e.g. 2000+ source files).
            const int maxConcurrency = 64;
            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            var readTasks = filePaths.Select(async path =>
            {
                await semaphore.WaitAsync();
                try { return await TryReadSourceFileAsync(path); }
                finally { semaphore.Release(); }
            });

            var sources = await Task.WhenAll(readTasks);
            return [.. sources.Where(s => s != null).Select(s => s!)];
        }

        private bool IsExcluded(string rootDirectory, string filePath)
        {
            var relativePath = Path.GetRelativePath(rootDirectory, filePath);
            var segments = relativePath.Split(Path.DirectorySeparatorChar);
            return segments.Any(segment => _exclusionFilters.Contains(segment, StringComparer.OrdinalIgnoreCase));
        }

        private static async Task<SourceFile?> TryReadSourceFileAsync(string file)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var rawLines = content.Split('\n');
                var lines = rawLines
                    .Select((line, index) => new LineOfCode(index + 1, line.TrimEnd('\r')))
                    .ToList();

                return new SourceFile(file, Path.GetFileName(file), content, lines);
            }
            catch (IOException ex)
            {
                await Console.Error.WriteLineAsync($"[Warning] Cannot read file {file}: {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                await Console.Error.WriteLineAsync($"[Warning] Access denied: {file}");
                return null;
            }
        }
    }
}
