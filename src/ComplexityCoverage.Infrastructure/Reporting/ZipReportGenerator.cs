using System.Globalization;
using System.IO.Compression;
using System.Text;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Infrastructure.Reporting
{
    /// <summary>
    /// Report generator that writes a ZIP archive containing:
    ///   - <c>coverage-report.html</c> at the root (the HTML summary)
    ///   - One annotated HTML per source file, organised by project folder
    /// </summary>
    public class ZipReportGenerator : IReportGenerator
    {
        private readonly ThemeDefinition _theme;
        private readonly HtmlReportGenerator _htmlGenerator;

        public ZipReportGenerator(ThemeDefinition? theme = null)
        {
            _theme = theme ?? ThemeDefinition.DarkMonokai;
            _htmlGenerator = new HtmlReportGenerator(_theme);
        }

        public async Task GenerateReportAsync(WeightedReport report, string outputPath)
        {
            // Build the summary HTML in memory (no standalone file written)
            var summaryHtml = await _htmlGenerator.BuildHtmlAsync(report);

            // Write the ZIP archive
            var zipPath = Path.ChangeExtension(outputPath, ".zip");
            await BuildZipAsync(report, zipPath, summaryHtml, Path.GetFileName(outputPath), _theme);
        }

        private static async Task BuildZipAsync(WeightedReport report, string zipPath,
            string summaryHtml, string summaryFileName, ThemeDefinition theme)
        {
            using var zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);

            // Root summary HTML
            var summaryEntry = archive.CreateEntry(summaryFileName, CompressionLevel.Optimal);
            await using (var se = summaryEntry.Open())
                await se.WriteAsync(Encoding.UTF8.GetBytes(summaryHtml));

            var sourceDetails = report.SourceDetails;
            if (sourceDetails is { Count: > 0 })
            {
                var weightLookup = report.FileDetails
                    .ToDictionary(f => f.FilePath, StringComparer.OrdinalIgnoreCase);

                foreach (var fileDetail in sourceDetails)
                {
                    var entryName = BuildEntryName(fileDetail.FilePath);
                    var html = BuildFileHtml(fileDetail, report.StrategyNames,
                        weightLookup.TryGetValue(fileDetail.FilePath, out var wd) ? wd : null,
                        theme);

                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await entryStream.WriteAsync(Encoding.UTF8.GetBytes(html));
                }
            }
        }

        private static string BuildEntryName(string filePath)
        {
            var projectFolder = ResolveProjectFolderName(filePath);
            var fileName = Path.GetFileName(filePath);
            return $"{projectFolder}/{fileName}.html";
        }

        /// <summary>
        /// Walks up the directory tree from <paramref name="filePath"/> to find the nearest
        /// folder that contains a <c>.csproj</c> file. Returns that folder's name.
        /// Falls back to the immediate parent folder name if no project is found.
        /// </summary>
        private static string ResolveProjectFolderName(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            while (dir is not null)
            {
                if (Directory.EnumerateFiles(dir, "*.csproj").Any())
                    return Path.GetFileName(dir) ?? "unknown";
                dir = Path.GetDirectoryName(dir);
            }
            // Fallback: immediate parent folder name
            return Path.GetFileName(Path.GetDirectoryName(filePath) ?? string.Empty) ?? "unknown";
        }

        private static string BuildFileHtml(FileSourceDetails fileDetail,
            IReadOnlyList<string> strategyNames, FileWeightDetails? weightDetails,
            ThemeDefinition t)
        {
            var sb = new StringBuilder();
            var fileName = Path.GetFileName(fileDetail.FilePath);

            var rawSource = string.Join("\n", fileDetail.Lines.Select(l => l.RawText));
            var highlightedLines = CSharpSyntaxHighlighter.BuildHighlightedLines(rawSource, t);

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine($"<title>{HtmlEncode(fileName)}</title>");
            sb.AppendLine("<meta charset=\"utf-8\"/>");
            sb.AppendLine("<style>");
            sb.AppendLine($"html, body {{ height: 100%; margin: 0; overflow: hidden; font-family: {t.FontFamily}; font-size: {t.FontSize}; background: {t.BodyBg}; color: {t.BodyFg}; }}");
            sb.AppendLine($".header {{ padding: 12px 20px; background: {t.HeaderBg}; border-bottom: 1px solid {t.HeaderBorder}; flex-shrink: 0; }}");
            sb.AppendLine($".header h1 {{ margin: 0 0 6px 0; font-size: {t.HeaderFontSize}; color: {t.SyntaxDefault}; }}");
            sb.AppendLine(".summary-cards { display: flex; gap: 10px; flex-wrap: wrap; }");
            sb.AppendLine($".card {{ padding: 4px 12px; border-radius: 4px; font-size: 0.85em; background: {t.CardStrategyBg}; color: {t.SyntaxDefault}; }}");
            sb.AppendLine(".page { display: flex; flex-direction: column; height: 100vh; }");
            sb.AppendLine(".table-wrap { flex: 1; overflow: auto; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
            sb.AppendLine($"td {{ padding: 1px 6px; white-space: pre; vertical-align: top; border-bottom: 1px solid {t.RowBorder}; }}");
            sb.AppendLine($".ln {{ color: {t.GutterFg}; text-align: right; user-select: none; min-width: 40px; border-right: 1px solid {t.GutterBorder}; }}");
            sb.AppendLine($".cov {{ text-align: right; min-width: 60px; border-right: 1px solid {t.GutterBorder}; color: {t.SyntaxDefault}; }}");
            sb.AppendLine(".src { }");
            sb.AppendLine($"tr.covered td {{ background-color: {t.CoveredBg}; }}");
            sb.AppendLine($"tr.uncovered td {{ background-color: {t.UncoveredBg}; }}");
            sb.AppendLine("tr.neutral td { background-color: transparent; }");
            sb.AppendLine($"thead td {{ position: sticky; top: 0; z-index: 1; background: {t.StickyHeaderBg}; }}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"page\">");
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
            sb.AppendLine("<div class=\"table-wrap\">");
            sb.AppendLine("<table>");

            sb.Append($"<thead><tr style=\"color:{t.SyntaxDefault};font-size:{t.HeaderFontSize};\">");
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

                var srcHtml = highlightedLines.TryGetValue(line.LineNumber, out var hl) ? hl : HtmlEncode(line.RawText);
                sb.AppendLine($"<td class=\"src\">{srcHtml}</td></tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
            sb.AppendLine("</div>"); // .table-wrap
            sb.AppendLine("</div>"); // .page
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private static string HtmlEncode(string text)
            => System.Net.WebUtility.HtmlEncode(text);
    }
}
