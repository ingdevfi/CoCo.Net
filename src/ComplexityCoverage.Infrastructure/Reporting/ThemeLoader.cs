using System.Text.Json;

namespace ComplexityCoverage.Infrastructure.Reporting
{
    /// <summary>
    /// Loads a <see cref="ThemeDefinition"/> from a JSON file on disk.
    ///
    /// Resolution order for <c>--theme &lt;value&gt;</c>:
    ///   1. Treat <paramref name="themePathOrName"/> as a full file path — use it as-is if the file exists.
    ///   2. Look for <c>themes/&lt;value&gt;.json</c> relative to the directory that contains the running executable.
    ///   3. Match against the two built-in theme names: <c>light</c> and <c>dark-monokai</c>.
    ///   4. Fall back to <see cref="ThemeDefinition.DarkMonokai"/> and log a warning.
    /// </summary>
    public static class ThemeLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>
        /// Resolves and loads the theme.
        /// </summary>
        /// <param name="themePathOrName">
        ///   Path to a <c>.json</c> file, a built-in name (<c>light</c> / <c>dark-monokai</c>),
        ///   or <c>null</c> to use the default (<c>dark-monokai</c>).
        /// </param>
        public static ThemeDefinition Load(string? themePathOrName = null)
        {
            if (string.IsNullOrWhiteSpace(themePathOrName))
                return ThemeDefinition.DarkMonokai;

            // 1. Absolute or relative file path
            if (File.Exists(themePathOrName))
                return LoadFile(themePathOrName) ?? ThemeDefinition.DarkMonokai;

            // 2. themes/<name>.json next to the executable
            var exeDir = Path.GetDirectoryName(Environment.ProcessPath ?? string.Empty) ?? Directory.GetCurrentDirectory();
            var candidate = Path.Combine(exeDir, "themes", themePathOrName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? themePathOrName
                : themePathOrName + ".json");

            if (File.Exists(candidate))
                return LoadFile(candidate) ?? ThemeDefinition.DarkMonokai;

            // 3. Built-in names
            return themePathOrName.ToLowerInvariant() switch
            {
                "light"        => ThemeDefinition.Light,
                "dark-monokai" => ThemeDefinition.DarkMonokai,
                _ => FallbackWithWarning(themePathOrName),
            };
        }

        /// <summary>Returns the names of available built-in themes.</summary>
        public static IReadOnlyList<string> BuiltInNames { get; } = ["light", "dark-monokai"];

        private static ThemeDefinition? LoadFile(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<ThemeDefinition>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Theme] Failed to load '{path}': {ex.Message} — using default theme.");
                return null;
            }
        }

        private static ThemeDefinition FallbackWithWarning(string name)
        {
            Console.Error.WriteLine($"[Theme] Unknown theme '{name}'. Built-in themes: {string.Join(", ", BuiltInNames)}. Using 'dark-monokai'.");
            return ThemeDefinition.DarkMonokai;
        }
    }
}
