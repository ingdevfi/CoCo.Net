using System.Text.Json;
using System.Text.Json.Nodes;
using ComplexityCoverage.Infrastructure.Reporting;

namespace ComplexityCoverage.Cli.Config
{
    /// <summary>
    /// Locates and deserializes the <c>coco.config.json</c> configuration file.
    /// </summary>
    public static class ConfigLoader
    {
        private const string DefaultFileName = "coco.config.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>
        /// Loads a <see cref="CliConfig"/> from <paramref name="path"/> if provided,
        /// otherwise looks for <c>coco.config.json</c> in the current working directory.
        /// Returns <c>null</c> if no file is found.
        /// </summary>
        public static CliConfig? Load(string? path = null)
        {
            var resolved = Resolve(path);
            if (resolved is null)
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(resolved);
                return JsonSerializer.Deserialize<CliConfig>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: could not load config file '{resolved}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Applies <see cref="CliConfig.ThemeOverrides"/> on top of a base <see cref="ThemeDefinition"/>
        /// by round-tripping through JSON so the caller gets a new theme instance with the overridden values.
        /// </summary>
        public static ThemeDefinition ApplyOverrides(ThemeDefinition baseTheme, Dictionary<string, string> overrides)
        {
            // Serialize the base theme to a JSON object
            var baseJson = JsonSerializer.Serialize(baseTheme, JsonOptions);
            var node = JsonNode.Parse(baseJson)!.AsObject();

            // Apply every override key (case-insensitive match against camelCase property names)
            foreach (var (key, value) in overrides)
            {
                // Find the matching key in the serialised object (handles casing)
                var match = node.FirstOrDefault(kv =>
                    string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));

                if (match.Key is not null)
                {
                    node[match.Key] = JsonValue.Create(value);
                }
            }

            var merged = node.ToJsonString();
            return JsonSerializer.Deserialize<ThemeDefinition>(merged, JsonOptions) ?? baseTheme;
        }

        private static string? Resolve(string? path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"Warning: config file '{path}' not found — ignoring.");
                }

                return File.Exists(path) ? path : null;
            }

            var cwd = Path.Combine(Directory.GetCurrentDirectory(), DefaultFileName);
            return File.Exists(cwd) ? cwd : null;
        }
    }
}
