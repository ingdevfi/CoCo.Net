using System.Text.Json.Serialization;

namespace ComplexityCoverage.Cli.Config
{
    /// <summary>
    /// Represents the optional <c>coco.config.json</c> configuration file.
    /// All properties are nullable; only non-null values are applied, so CLI arguments
    /// always take precedence over config-file values.
    /// </summary>
    public sealed class CliConfig
    {
        /// <summary>Path to the solution file (.sln or .slnx).</summary>
        [JsonPropertyName("solution")]
        public string? Solution { get; init; }

        /// <summary>Path to a specific test project file (optional).</summary>
        [JsonPropertyName("testProject")]
        public string? TestProject { get; init; }

        /// <summary>Output report path (default: coverage-report.html).</summary>
        [JsonPropertyName("output")]
        public string? Output { get; init; }

        /// <summary>Output mode: console | html | zip | zip+console (default: html).</summary>
        [JsonPropertyName("outputMode")]
        public string? OutputMode { get; init; }

        /// <summary>Complexity strategy: mccabe, nesting, halstead, mi, all, or comma-separated (default: mi).</summary>
        [JsonPropertyName("complexity")]
        public string? Complexity { get; init; }

        /// <summary>Test execution timeout in minutes (default: 15).</summary>
        [JsonPropertyName("timeout")]
        public int? Timeout { get; init; }

        /// <summary>Path to an existing coverage file (skips test run).</summary>
        [JsonPropertyName("coverageFile")]
        public string? CoverageFile { get; init; }

        /// <summary>Coverage file format: cobertura | opencover (auto-detected if omitted).</summary>
        [JsonPropertyName("coverageFormat")]
        public string? CoverageFormat { get; init; }

        /// <summary>Theme name (light | dark-monokai) or path to a custom theme JSON file.</summary>
        [JsonPropertyName("theme")]
        public string? Theme { get; init; }

        /// <summary>
        /// Overrides individual theme colour/font properties without creating a full custom theme file.
        /// Keys are the camelCase JSON property names of <see cref="ComplexityCoverage.Infrastructure.Reporting.ThemeDefinition"/>
        /// (e.g. <c>bodyBg</c>, <c>syntaxKeyword</c>, <c>fontFamily</c>).
        /// </summary>
        [JsonPropertyName("themeOverrides")]
        public Dictionary<string, string>? ThemeOverrides { get; init; }
    }
}
