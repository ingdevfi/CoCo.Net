using System.Text.Json.Serialization;

namespace ComplexityCoverage.Infrastructure.Reporting
{
    /// <summary>
    /// Centralises every colour used across both the HTML summary report and the
    /// per-file annotated source reports inside the ZIP archive.
    ///
    /// Two built-in themes are provided as static fallbacks when no external file is found:
    ///   <see cref="Light"/>  – clean light theme
    ///   <see cref="DarkMonokai"/> – Monokai-inspired dark theme
    ///
    /// Users can create or edit a JSON file with the same property names to customise
    /// the output without recompiling the tool.
    /// </summary>
    public record ThemeDefinition
    {
        // ── General UI ────────────────────────────────────────────────────────

        [JsonPropertyName("name")]
        public string Name { get; init; } = "Custom";

        /// <summary>Font family used for all text (source code + UI).</summary>
        [JsonPropertyName("fontFamily")]
        public string FontFamily { get; init; } = "'Consolas', 'Courier New', monospace";

        /// <summary>Base font size (e.g. "13px", "0.9rem").</summary>
        [JsonPropertyName("fontSize")]
        public string FontSize { get; init; } = "13px";

        /// <summary>Font size for page/section headers (e.g. "1.2em", "16px").</summary>
        [JsonPropertyName("headerFontSize")]
        public string HeaderFontSize { get; init; } = "1.2em";

        /// <summary>Page / body background.</summary>
        [JsonPropertyName("bodyBg")]
        public string BodyBg { get; init; } = "#ffffff";

        /// <summary>Default text colour.</summary>
        [JsonPropertyName("bodyFg")]
        public string BodyFg { get; init; } = "#24292e";

        /// <summary>Toolbar / header bar background.</summary>
        [JsonPropertyName("headerBg")]
        public string HeaderBg { get; init; } = "#f6f8fa";

        /// <summary>Header border colour.</summary>
        [JsonPropertyName("headerBorder")]
        public string HeaderBorder { get; init; } = "#d0d7de";

        /// <summary>Header title / path text colour.</summary>
        [JsonPropertyName("headerFg")]
        public string HeaderFg { get; init; } = "#57606a";

        /// <summary>Summary card background (line coverage).</summary>
        [JsonPropertyName("cardLineBg")]
        public string CardLineBg { get; init; } = "#0969da";

        /// <summary>Summary card background (strategy).</summary>
        [JsonPropertyName("cardStrategyBg")]
        public string CardStrategyBg { get; init; } = "#1f6feb";

        /// <summary>Summary card text colour.</summary>
        [JsonPropertyName("cardFg")]
        public string CardFg { get; init; } = "#ffffff";

        // ── Summary table (HTML report) ───────────────────────────────────────

        [JsonPropertyName("tableBorder")]
        public string TableBorder { get; init; } = "#d0d7de";

        [JsonPropertyName("tableHeaderBg")]
        public string TableHeaderBg { get; init; } = "#0969da";

        [JsonPropertyName("tableHeaderFg")]
        public string TableHeaderFg { get; init; } = "#ffffff";

        [JsonPropertyName("tableRowAltBg")]
        public string TableRowAltBg { get; init; } = "#f6f8fa";

        // ── Source file view (ZIP) ────────────────────────────────────────────

        /// <summary>Row background when line is covered by tests.</summary>
        [JsonPropertyName("coveredBg")]
        public string CoveredBg { get; init; } = "#d4edda";

        /// <summary>Row background when line is NOT covered by tests.</summary>
        [JsonPropertyName("uncoveredBg")]
        public string UncoveredBg { get; init; } = "#f8d7da";

        /// <summary>Line-number gutter text colour.</summary>
        [JsonPropertyName("gutterFg")]
        public string GutterFg { get; init; } = "#8b949e";

        /// <summary>Line-number gutter / complexity column border.</summary>
        [JsonPropertyName("gutterBorder")]
        public string GutterBorder { get; init; } = "#d0d7de";

        /// <summary>Complexity column text colour.</summary>
        [JsonPropertyName("complexityFg")]
        public string ComplexityFg { get; init; } = "#57606a";

        /// <summary>Row separator colour.</summary>
        [JsonPropertyName("rowBorder")]
        public string RowBorder { get; init; } = "#eaecef";

        /// <summary>Sticky header background in source view.</summary>
        [JsonPropertyName("stickyHeaderBg")]
        public string StickyHeaderBg { get; init; } = "#f6f8fa";

        // ── Syntax highlighting ───────────────────────────────────────────────

        [JsonPropertyName("syntaxKeyword")]
        public string SyntaxKeyword { get; init; } = "#0550ae";

        [JsonPropertyName("syntaxControlFlow")]
        public string SyntaxControlFlow { get; init; } = "#8250df";

        [JsonPropertyName("syntaxString")]
        public string SyntaxString { get; init; } = "#0a3069";

        [JsonPropertyName("syntaxNumber")]
        public string SyntaxNumber { get; init; } = "#0550ae";

        [JsonPropertyName("syntaxComment")]
        public string SyntaxComment { get; init; } = "#6e7781";

        [JsonPropertyName("syntaxPreproc")]
        public string SyntaxPreproc { get; init; } = "#953800";

        [JsonPropertyName("syntaxType")]
        public string SyntaxType { get; init; } = "#116329";

        [JsonPropertyName("syntaxDefault")]
        public string SyntaxDefault { get; init; } = "#24292e";

        // ── Built-in themes ───────────────────────────────────────────────────

        /// <summary>Clean GitHub-inspired light theme.</summary>
        public static ThemeDefinition Light { get; } = new()
        {
            Name             = "light",
            FontFamily       = "'Segoe UI', Arial, sans-serif",
            FontSize         = "14px",
            HeaderFontSize   = "1.2em",
            BodyBg           = "#ffffff",
            BodyFg           = "#24292e",
            HeaderBg         = "#f6f8fa",
            HeaderBorder     = "#d0d7de",
            HeaderFg         = "#57606a",
            CardLineBg       = "#0969da",
            CardStrategyBg   = "#1f6feb",
            CardFg           = "#ffffff",
            TableBorder      = "#d0d7de",
            TableHeaderBg    = "#0969da",
            TableHeaderFg    = "#ffffff",
            TableRowAltBg    = "#f6f8fa",
            CoveredBg        = "#d4edda",
            UncoveredBg      = "#f8d7da",
            GutterFg         = "#8b949e",
            GutterBorder     = "#d0d7de",
            ComplexityFg     = "#57606a",
            RowBorder        = "#eaecef",
            StickyHeaderBg   = "#f6f8fa",
            SyntaxKeyword    = "#0550ae",
            SyntaxControlFlow= "#8250df",
            SyntaxString     = "#0a3069",
            SyntaxNumber     = "#0550ae",
            SyntaxComment    = "#6e7781",
            SyntaxPreproc    = "#953800",
            SyntaxType       = "#116329",
            SyntaxDefault    = "#24292e",
        };

        /// <summary>Monokai-inspired dark theme.</summary>
        public static ThemeDefinition DarkMonokai { get; } = new()
        {
            Name             = "dark-monokai",
            FontFamily       = "'Cascadia Code', 'Fira Code', 'Consolas', monospace",
            FontSize         = "13px",
            HeaderFontSize   = "1.1em",
            BodyBg           = "#272822",
            BodyFg           = "#f8f8f2",
            HeaderBg         = "#1e1f1c",
            HeaderBorder     = "#3e3d32",
            HeaderFg         = "#a59f85",
            CardLineBg       = "#75715e",
            CardStrategyBg   = "#49483e",
            CardFg           = "#f8f8f2",
            TableBorder      = "#3e3d32",
            TableHeaderBg    = "#49483e",
            TableHeaderFg    = "#f8f8f2",
            TableRowAltBg    = "#2d2e27",
            CoveredBg        = "#1a3a1a",
            UncoveredBg      = "#3a1a1a",
            GutterFg         = "#75715e",
            GutterBorder     = "#3e3d32",
            ComplexityFg     = "#75715e",
            RowBorder        = "#3e3d32",
            StickyHeaderBg   = "#1e1f1c",
            SyntaxKeyword    = "#f92672",   // Monokai pink/red
            SyntaxControlFlow= "#ae81ff",   // Monokai purple
            SyntaxString     = "#e6db74",   // Monokai yellow
            SyntaxNumber     = "#ae81ff",   // Monokai purple
            SyntaxComment    = "#75715e",   // Monokai grey-brown
            SyntaxPreproc    = "#a6e22e",   // Monokai green
            SyntaxType       = "#a6e22e",   // Monokai green
            SyntaxDefault    = "#f8f8f2",   // Monokai white
        };
    }
}
