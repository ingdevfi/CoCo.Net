using System.Globalization;
using System.Text;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Infrastructure.Reporting
{
    public class HtmlReportGenerator : IReportGenerator
    {
        private readonly ThemeDefinition _theme;

        public HtmlReportGenerator(ThemeDefinition? theme = null)
        {
            _theme = theme ?? ThemeDefinition.DarkMonokai;
        }

        public async Task GenerateReportAsync(WeightedReport report, string outputPath)
        {
            var html = BuildHtml(report, _theme);
            await File.WriteAllTextAsync(outputPath, html);
        }

        public Task<string> BuildHtmlAsync(WeightedReport report)
            => Task.FromResult(BuildHtml(report, _theme));

        private static string BuildHtml(WeightedReport report, ThemeDefinition t)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<title>Complexity-Weighted Coverage Report</title>");
            sb.AppendLine("<meta charset=\"utf-8\"/>");
            sb.AppendLine("<style>");
            sb.AppendLine($"body {{ font-family: {t.FontFamily}; font-size: {t.FontSize}; margin: 20px; background: {t.BodyBg}; color: {t.BodyFg}; }}");
            sb.AppendLine($"h1, h2 {{ font-size: {t.HeaderFontSize}; }}");
            sb.AppendLine($".summary {{ display: flex; gap: 15px; margin-bottom: 20px; flex-wrap: wrap; }}");
            sb.AppendLine($".summary-card {{ padding: 15px 25px; border-radius: 8px; color: {t.CardFg}; }}");
            sb.AppendLine($".card-line {{ background-color: {t.CardLineBg}; }}");
            sb.AppendLine($".card-strategy {{ background-color: {t.CardStrategyBg}; }}");
            sb.AppendLine($"table {{ border-collapse: collapse; width: 100%; margin-bottom: 20px; }}");
            sb.AppendLine($"th, td {{ border: 1px solid {t.TableBorder}; padding: 8px; text-align: left; }}");
            sb.AppendLine($"th {{ background-color: {t.TableHeaderBg}; color: {t.TableHeaderFg}; position: sticky; top: 0; z-index: 1; }}");
            sb.AppendLine($"tr:nth-child(even) td {{ background-color: {t.TableRowAltBg}; }}");
            sb.AppendLine(".num { text-align: right; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine($"<body>");
            sb.AppendLine("<h1>Complexity-Weighted Coverage Report</h1>");
            sb.AppendLine("<div class=\"summary\">");
            sb.AppendLine($"<div class=\"summary-card card-line\"><strong>Line Coverage:</strong> {report.OverallLineCoveragePercentage.ToString("F2", CultureInfo.InvariantCulture)}%</div>");
            foreach (var name in report.StrategyNames)
            {
                var pct = report.OverallWeightedCoverageByStrategy.GetValueOrDefault(name, 0);
                sb.AppendLine($"<div class=\"summary-card card-strategy\"><strong>{name}:</strong> {pct.ToString("F2", CultureInfo.InvariantCulture)}%</div>");
            }
            sb.AppendLine("</div>");
            sb.AppendLine("<h2>File Details</h2>");
            sb.AppendLine("<table>");
            sb.Append("<thead><tr><th>File</th><th class=\"num\">Line Coverage</th>");
            foreach (var name in report.StrategyNames)
                sb.Append($"<th class=\"num\">{name}</th>");
            sb.AppendLine("</tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var detail in report.FileDetails)
            {
                var fileName = Path.GetFileName(detail.FilePath);
                sb.Append($"<tr><td>{fileName}</td><td class=\"num\">{detail.LineCoveragePercentage.ToString("F2", CultureInfo.InvariantCulture)}%</td>");
                foreach (var name in report.StrategyNames)
                {
                    var pct = detail.WeightedCoverageByStrategy.GetValueOrDefault(name, 0);
                    sb.Append($"<td class=\"num\">{pct.ToString("F2", CultureInfo.InvariantCulture)}%</td>");
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
            sb.AppendLine($"<hr style=\"border-color:{t.TableBorder}\"/>");
            var totalFiles = report.FileDetails.Count;
            sb.Append($"<p style=\"color:{t.GutterFg};font-size:0.9em;\">{totalFiles} files &middot; {report.TotalLines} lines");
            if (report.Duration.HasValue)
                sb.Append($" &middot; Generated in {report.Duration.Value.TotalSeconds:F1}s");
            sb.AppendLine("</p>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
