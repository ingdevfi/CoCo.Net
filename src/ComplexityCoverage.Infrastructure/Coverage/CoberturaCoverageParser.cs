using System.Xml.Linq;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Infrastructure.Coverage
{
    public class CoberturaCoverageParser : ICoberturaCoverageProvider, ICoverageFileParser
    {
        public async Task<CoverageMap> ParseAsync(string xmlFilePath)
        {
            var fileCoverage = new Dictionary<string, Dictionary<int, bool>>();

            var root = await LoadDocumentRootAsync(xmlFilePath);
            if (root == null)
            {
                return new CoverageMap(new Dictionary<string, IReadOnlyDictionary<int, bool>>());
            }

            // Resolve base directory from <sources> element or XML file directory as fallback
            var baseDir = GetSourcesBaseDirectory(root)
                ?? Path.GetDirectoryName(Path.GetFullPath(xmlFilePath));

            foreach (var classElement in GetClassElements(root))
            {
                var fileNameAttr = classElement.Attribute("filename");
                if (fileNameAttr == null)
                {
                    continue;
                }

                var filePath = ResolveFilePath(fileNameAttr.Value, baseDir);
                var lineCoverage = ParseLineCoverage(classElement);

                if (lineCoverage.Count > 0)
                {
                    MergeFileCoverage(fileCoverage, filePath, lineCoverage);
                }
            }

            var result = fileCoverage.ToDictionary(kv => kv.Key, kv => (IReadOnlyDictionary<int, bool>)kv.Value);
            return new CoverageMap(result);
        }

        /// <summary>
        /// Extracts the base directory from the Cobertura XML sources element.
        /// </summary>
        private static string? GetSourcesBaseDirectory(XElement root)
        {
            var sourceElement = root.Descendants("source").FirstOrDefault();
            if (sourceElement == null)
            {
                return null;
            }

            var sourceDir = sourceElement.Value.Trim();
            return string.IsNullOrEmpty(sourceDir) ? null : sourceDir;
        }

        /// <summary>
        /// Resolves a file path from the coverage XML to an absolute, normalized path.
        /// Only resolves relative paths when a base directory is available and the path
        /// contains directory traversal (../) or is clearly relative.
        /// </summary>
        private static string ResolveFilePath(string filePath, string? baseDir)
        {
            // Normalize separators
            filePath = filePath.Replace('/', Path.DirectorySeparatorChar)
                               .Replace('\\', Path.DirectorySeparatorChar);

            // If already absolute, just normalize
            if (Path.IsPathRooted(filePath))
            {
                return Path.GetFullPath(filePath);
            }

            // Resolve relative paths using base directory
            if (baseDir != null)
            {
                return Path.GetFullPath(Path.Combine(baseDir, filePath));
            }

            // No base directory available — keep as-is
            return filePath;
        }

        private static async Task<XElement?> LoadDocumentRootAsync(string xmlFilePath)
        {
            using var stream = File.OpenRead(xmlFilePath);
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
            return doc.Root;
        }

        private static IEnumerable<XElement> GetClassElements(XElement root)
            => root.Descendants("class");

        private static Dictionary<int, bool> ParseLineCoverage(XElement classElement)
        {
            var lineCoverage = new Dictionary<int, bool>();

            foreach (var lineElement in classElement.Descendants("line"))
            {
                if (TryParseLineElement(lineElement, out var lineNumber, out var isCovered))
                {
                    lineCoverage[lineNumber] = isCovered;
                }
            }

            return lineCoverage;
        }

        private static bool TryParseLineElement(XElement lineElement, out int lineNumber, out bool isCovered)
        {
            lineNumber = 0;
            isCovered = false;

            var numberAttr = lineElement.Attribute("number");
            var hitsAttr = lineElement.Attribute("hits");
            if (numberAttr == null || hitsAttr == null)
            {
                return false;
            }

            if (!int.TryParse(numberAttr.Value, out lineNumber))
            {
                return false;
            }

            if (!int.TryParse(hitsAttr.Value, out var hits))
            {
                return false;
            }

            isCovered = hits > 0;
            return true;
        }

        private static void MergeFileCoverage(Dictionary<string, Dictionary<int, bool>> fileCoverage, string filePath, Dictionary<int, bool> lineCoverage)
        {
            if (!fileCoverage.TryGetValue(filePath, out var existing))
            {
                fileCoverage[filePath] = new Dictionary<int, bool>(lineCoverage);
                return;
            }

            foreach (var entry in lineCoverage)
            {
                existing[entry.Key] = entry.Value;
            }
        }
    }
}
