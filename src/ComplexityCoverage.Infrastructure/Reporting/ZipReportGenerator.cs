using System.Globalization;
using System.IO.Compression;
using System.Text;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Infrastructure.Reporting
{
    /// <summary>
    /// Report generator that writes:
    ///   - The standard HTML summary report (same as HtmlReportGenerator)
    ///   - A ZIP archive (same base name, .zip extension) containing one annotated HTML per source file
    /// </summary>
    public class ZipReportGenerator : IReportGenerator
    {
        private readonly HtmlReportGenerator _htmlGenerator = new();

        public async Task GenerateReportAsync(WeightedReport report, string outputPath)
        {
            // 1. Write the summary HTML report (standard behaviour)
            await _htmlGenerator.GenerateReportAsync(report, outputPath);

            // 2. Write the ZIP archive
            var zipPath = Path.ChangeExtension(outputPath, ".zip");
            await BuildZipAsync(report, zipPath);
        }

        private static async Task BuildZipAsync(WeightedReport report, string zipPath)
        {
            var sourceDetails = report.SourceDetails;
            if (sourceDetails == null || sourceDetails.Count == 0)
                return;

            // Build a lookup for weight details by file path
            var weightLookup = report.FileDetails
                .ToDictionary(f => f.FilePath, StringComparer.OrdinalIgnoreCase);

            using var zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);

            foreach (var fileDetail in sourceDetails)
            {
                var entryName = BuildEntryName(fileDetail.FilePath);
                var html = BuildFileHtml(fileDetail, report.StrategyNames,
                    weightLookup.TryGetValue(fileDetail.FilePath, out var wd) ? wd : null);

                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(Encoding.UTF8.GetBytes(html));
            }
        }

        private static string BuildEntryName(string filePath)
        {
            // Use the last two path segments to avoid name collisions while keeping context
            var parts = filePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var name = parts.Length >= 2
                ? string.Join("_", parts[^2], parts[^1])
                : parts[^1];
            return name + ".html";
        }

        private static string BuildFileHtml(FileSourceDetails fileDetail,
            IReadOnlyList<string> strategyNames, FileWeightDetails? weightDetails)
        {
            var sb = new StringBuilder();
            var fileName = Path.GetFileName(fileDetail.FilePath);

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine($"<title>{HtmlEncode(fileName)}</title>");
            sb.AppendLine("<meta charset=\"utf-8\"/>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Consolas', monospace; font-size: 13px; margin: 0; background: #1e1e1e; color: #d4d4d4; }");
            sb.AppendLine(".header { padding: 12px 20px; background: #252526; border-bottom: 1px solid #3c3c3c; }");
            sb.AppendLine(".header h1 { margin: 0 0 6px 0; font-size: 1em; color: #ccc; }");
            sb.AppendLine(".summary-cards { display: flex; gap: 10px; flex-wrap: wrap; }");
            sb.AppendLine(".card { padding: 4px 12px; border-radius: 4px; font-size: 0.85em; background: #1976D2; color: #fff; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
            sb.AppendLine("td { padding: 1px 6px; white-space: pre; vertical-align: top; border-bottom: 1px solid #2a2a2a; }");
            sb.AppendLine(".ln { color: #858585; text-align: right; user-select: none; min-width: 40px; border-right: 1px solid #3c3c3c; }");
            sb.AppendLine(".cov { text-align: right; min-width: 60px; border-right: 1px solid #3c3c3c; font-size: 0.8em; color: #888; }");
            sb.AppendLine(".src { }");
            sb.AppendLine("tr.covered { background-color: #1a3a1a; }");
            sb.AppendLine("tr.uncovered { background-color: #3a1a1a; }");
            sb.AppendLine("tr.neutral { background-color: transparent; }");
            sb.AppendLine("thead td { position: sticky; top: 0; z-index: 1; background: #252526; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine($"<h1>{HtmlEncode(fileDetail.FilePath)}</h1>");
            sb.AppendLine("<div class=\"summary-cards\">");
            if (weightDetails != null)
            {
                sb.AppendLine($"<span class=\"card\">Line Coverage: {weightDetails.LineCoveragePercentage.ToString("F1", CultureInfo.InvariantCulture)}%</span>");
                foreach (var name in strategyNames)
                {
                    var pct = weightDetails.WeightedCoverageByStrategy.GetValueOrDefault(name, 0);
                    sb.AppendLine($"<span class=\"card\">{HtmlEncode(name)}: {pct.ToString("F1", CultureInfo.InvariantCulture)}%</span>");
                }
            }
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<table>");

            // Sticky header row for complexity columns
            sb.Append("<thead><tr style=\"color:#888;font-size:0.8em;\">");
            sb.Append("<td class=\"ln\">#</td>");
            foreach (var name in strategyNames)
                sb.Append($"<td class=\"cov\">{HtmlEncode(name)}</td>");
            sb.AppendLine("<td class=\"src\">Source</td></tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var line in fileDetail.Lines)
            {
                var rowClass = line.IsCovered == true ? "covered" : line.IsCovered == false ? "uncovered" : "neutral";
                sb.Append($"<tr class=\"{rowClass}\">");
                sb.Append($"<td class=\"ln\">{line.LineNumber}</td>");
                foreach (var name in strategyNames)
                {
                    var w = line.ComplexityWeightByStrategy.GetValueOrDefault(name, 0);
                    var display = w > 0 ? w.ToString("F2", CultureInfo.InvariantCulture) : "";
                    sb.Append($"<td class=\"cov\">{display}</td>");
                }
                sb.AppendLine($"<td class=\"src\">{HtmlEncode(line.RawText)}</td></tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private static string HtmlEncode(string text)
            => System.Net.WebUtility.HtmlEncode(text);
    }
}
