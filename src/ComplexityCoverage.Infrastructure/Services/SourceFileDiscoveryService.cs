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

            // Read files concurrently; I/O-bound work benefits from parallel reads
            var readTasks = filePaths.Select(TryReadSourceFileAsync);
            var sources = await Task.WhenAll(readTasks);

            return [.. sources.Where(source => source != null).Select(source => source!)];
        }

        private static async Task<IEnumerable<string>> EnumerateFilesForExtensionAsync(string directory, string ext)
        {
            try
            {
                return Directory.EnumerateFiles(directory, $"*{ext}", SearchOption.AllDirectories);
            }
            catch (IOException ex)
            {
                await Console.Error.WriteLineAsync($"[Warning] Cannot enumerate directory {directory} for extension {ext}: {ex.Message}");
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                await Console.Error.WriteLineAsync($"[Warning] Access denied to directory {directory}");
                return Array.Empty<string>();
            }
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
