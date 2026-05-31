using ComplexityCoverage.Domain.Interfaces;

namespace ComplexityCoverage.Infrastructure.Coverage
{
    /// <summary>
    /// Resolves the appropriate ICoverageFileParser based on a format name or file extension.
    /// Supported formats: cobertura (.xml), opencover (.xml)
    /// </summary>
    public static class CoverageFileParserFactory
    {
        /// <summary>
        /// Returns the parser for the given format name.
        /// If <paramref name="format"/> is null or empty, auto-detects from <paramref name="filePath"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown when the format is not supported.</exception>
        public static ICoverageFileParser Resolve(string filePath, string? format = null)
        {
            var resolved = string.IsNullOrWhiteSpace(format)
                ? DetectFormat(filePath)
                : format.ToLowerInvariant().Trim();

            return resolved switch
            {
                "opencover" => new OpenCoverCoverageParser(),
                "cobertura" => new CoberturaCoverageParser(),
                _ => throw new NotSupportedException($"Coverage format '{resolved}' is not supported. Use 'cobertura' or 'opencover'.")
            };
        }

        private static string DetectFormat(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ext == ".trx")
                throw new NotSupportedException(
                    "TRX files are not supported as a coverage source. " +
                    "TRX files contain only test pass/fail results with no line-level coverage data. " +
                    "Use a Cobertura or OpenCover file instead.");

            // Both Cobertura and OpenCover use .xml — peek at root element
            if (ext == ".xml")
                return PeekXmlFormat(filePath);

            return "cobertura";
        }

        private static string PeekXmlFormat(string filePath)
        {
            try
            {
                using var reader = System.Xml.XmlReader.Create(filePath, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
                while (reader.Read())
                {
                    if (reader.NodeType != System.Xml.XmlNodeType.Element)
                        continue;

                    return reader.LocalName.ToLowerInvariant() switch
                    {
                        "coveragesession" => "opencover",
                        "coverage" => "cobertura",
                        _ => "cobertura"
                    };
                }
            }
            catch
            {
                // Fall back to Cobertura on any read error
            }

            return "cobertura";
        }
    }
}
