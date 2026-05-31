using System.Xml.Linq;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Infrastructure.Coverage
{
    /// <summary>
    /// Parses OpenCover XML coverage files into a CoverageMap.
    /// OpenCover maps files via &lt;File fullPath="..." uid="N"/&gt; elements,
    /// and coverage via &lt;SequencePoint sl="N" vc="N" fileid="N"/&gt; attributes.
    /// </summary>
    public class OpenCoverCoverageParser : ICoverageFileParser
    {
        public async Task<CoverageMap> ParseAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

            var root = doc.Root;
            if (root == null)
            {
                return new CoverageMap(new Dictionary<string, IReadOnlyDictionary<int, bool>>());
            }

            var fileIndex = BuildFileIndex(root);
            var fileCoverage = BuildFileCoverage(root, fileIndex);

            var result = fileCoverage.ToDictionary(kv => kv.Key, kv => (IReadOnlyDictionary<int, bool>)kv.Value);
            return new CoverageMap(result);
        }

        private static Dictionary<string, string> BuildFileIndex(XElement root)
        {
            // <File uid="1" fullPath="C:\src\Foo.cs" />
            return root.Descendants("File")
                .Where(f => f.Attribute("uid") != null && f.Attribute("fullPath") != null)
                .ToDictionary(
                    f => f.Attribute("uid")!.Value,
                    f => f.Attribute("fullPath")!.Value);
        }

        private static Dictionary<string, Dictionary<int, bool>> BuildFileCoverage(
            XElement root, Dictionary<string, string> fileIndex)
        {
            var fileCoverage = new Dictionary<string, Dictionary<int, bool>>();

            foreach (var sp in root.Descendants("SequencePoint"))
            {
                if (!TryParseSequencePoint(sp, fileIndex, out var filePath, out var lineNumber, out var isCovered))
                {
                    continue;
                }

                if (!fileCoverage.TryGetValue(filePath!, out var lines))
                {
                    lines = new Dictionary<int, bool>();
                    fileCoverage[filePath!] = lines;
                }

                // If any visit covers the line, mark it covered
                lines[lineNumber] = lines.TryGetValue(lineNumber, out var existing)
                    ? existing || isCovered
                    : isCovered;
            }

            return fileCoverage;
        }

        private static bool TryParseSequencePoint(
            XElement sp,
            Dictionary<string, string> fileIndex,
            out string? filePath,
            out int lineNumber,
            out bool isCovered)
        {
            filePath = null;
            lineNumber = 0;
            isCovered = false;

            var fileId = sp.Attribute("fileid")?.Value ?? sp.Attribute("fileId")?.Value;
            var slAttr = sp.Attribute("sl")?.Value;
            var vcAttr = sp.Attribute("vc")?.Value;

            if (fileId == null || slAttr == null || vcAttr == null)
            {
                return false;
            }

            if (!fileIndex.TryGetValue(fileId, out filePath))
            {
                return false;
            }

            if (!int.TryParse(slAttr, out lineNumber))
            {
                return false;
            }

            if (!int.TryParse(vcAttr, out var visitCount))
            {
                return false;
            }

            isCovered = visitCount > 0;
            return true;
        }
    }
}
