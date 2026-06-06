using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Tests
{
    /// <summary>
    /// Shared test utilities for complexity strategy tests.
    /// </summary>
    public static class ComplexityTestHelper
    {
        /// <summary>
        /// Parses a code string into a list of LineOfCode objects.
        /// </summary>
        public static List<LineOfCode> ParseLines(string content)
        {
            return [.. content.Split('\n').Select((text, index) => new LineOfCode(index + 1, text.TrimEnd('\r')))];
        }

        /// <summary>
        /// Creates a SourceFile from code string.
        /// </summary>
        public static SourceFile CreateSourceFile(string code)
        {
            var lines = ParseLines(code);
            return new SourceFile("test.cs", "test.cs", code, lines);
        }
    }
}

