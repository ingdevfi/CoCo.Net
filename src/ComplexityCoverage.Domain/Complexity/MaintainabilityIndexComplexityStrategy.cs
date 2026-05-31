using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;

namespace ComplexityCoverage.Domain.Complexity
{
    /// <summary>
    /// Calculates Maintainability Index (MI) complexity metric.
    ///
    /// THEORETICAL BACKGROUND:
    /// Maintainability Index is a composite metric that predicts how difficult a piece of code 
    /// will be to maintain over time. It combines three key metrics:
    ///
    /// Formula: MI = 171 - 5.2 * ln(HALVOL) - 0.23 * CYCLO - 10.2 * ln(SLOC)
    /// where:
    ///   HALVOL = Halstead Volume (information content and complexity)
    ///   CYCLO  = Cyclomatic Complexity (number of independent code paths)
    ///   SLOC   = Source Lines of Code (excluding comments and blank lines)
    ///
    /// Normalized formula: MI' = max(0, (MI * 100) / 171)
    /// Result range: 0-100, where higher values indicate better maintainability.
    ///
    /// INTERPRETATION:
    /// - 85-100: Highly maintainable (green)
    /// - 50-84:  Maintainable with some concern (yellow)  
    /// - <50:    Difficult to maintain (red)
    ///
    /// IMPLEMENTATION APPROACH:
    /// The MI is computed per line by combining:
    /// - Method-level McCabe cyclomatic complexity (shared across all lines of the method)
    /// - Line-level Halstead Volume (specific to each line's operators/operands)
    /// - Method-level SLOC (source lines of code)
    ///
    /// This produces a unique weight per line: lines with more operators/operands
    /// within a complex method receive a higher weight than simple lines in the same method.
    ///
    /// Since lower MI means worse code and the project expects higher weight = higher complexity,
    /// the weight is calculated as: weight = 100 - MI'
    /// This inverts the scale so that difficult-to-maintain code has higher weight.
    ///
    /// ADVANTAGES:
    /// - Composite metric capturing multiple complexity aspects
    /// - Widely adopted (used by Visual Studio, SonarQube)
    /// - Correlates with maintenance effort and defect rates
    /// - Single 0-100 scale for easy interpretation
    ///
    /// LIMITATIONS:
    /// - Depends on accurate Halstead Volume and Cyclomatic Complexity calculations
    /// - SLOC counting may vary based on style and formatting
    /// - Logarithmic formula can produce anomalies with very small values
    /// - May not account for code readability/documentation quality
    /// </summary>
    public class MaintainabilityIndexComplexityStrategy(
        IComplexityStrategy halsteadStrategy,
        IComplexityStrategy mccabeStrategy) : AbstractComplexityStrategy(new WrappingSyntaxTreeCache())
    {
        private readonly IComplexityStrategy _halsteadStrategy = halsteadStrategy ?? throw new ArgumentNullException(nameof(halsteadStrategy));
        private readonly IComplexityStrategy _mccabeStrategy = mccabeStrategy ?? throw new ArgumentNullException(nameof(mccabeStrategy));
        private readonly ConcurrentDictionary<SyntaxTree, Dictionary<MethodDeclarationSyntax, double>> _methodMICache = new();
        private readonly ConcurrentDictionary<SyntaxTree, IReadOnlyList<MethodSpan>> _methodSpanCache = new();

        protected override double CalculateLineWeight(int lineNumber, SyntaxNode root, SyntaxTree tree)
        {
            var containingMethod = FindMethodContainingLine(lineNumber, root, tree);
            if (containingMethod == null)
            {
                return 0.0;
            }

            var miCache = _methodMICache.GetOrAdd(tree, _ => []);
            if (!miCache.TryGetValue(containingMethod, out var mi))
            {
                mi = ComputeMethodMI(containingMethod, root, tree);
                miCache[containingMethod] = mi;
            }

            return mi;
        }

        private double ComputeMethodMI(MethodDeclarationSyntax method, SyntaxNode root, SyntaxTree tree)
        {
            var methodLines = GetMethodLines(method, tree);
            if (methodLines.Count == 0)
            {
                return 50.0;
            }

            // Build a shared SourceFile with full content for cache-friendly strategy calls
            var content = root.SyntaxTree.GetText().ToString();
            var lineOfCodes = methodLines.Select(ln => new LineOfCode(ln, "")).ToArray();
            var sourceFile = new SourceFile("temp.cs", "temp.cs", content, lineOfCodes);

            // McCabe is method-level: one call suffices
            double cyclo = _mccabeStrategy.CalculateWeight(lineOfCodes[0], sourceFile);

            // Halstead is line-level: get weights for all method lines and average
            var halsteadWeights = _halsteadStrategy.CalculateWeights(sourceFile);
            double halvol = halsteadWeights.Count > 0 ? halsteadWeights.Average() : 1.0;

            int sloc = CountSourceLinesOfCode(method);
            return CalculateMI(cyclo, halvol, sloc);
        }

        /// <summary>
        /// Computes the normalized inverted MI weight from the three input metrics.
        /// Formula: MI = 171 - 5.2 * ln(HALVOL) - 0.23 * CYCLO - 10.2 * ln(SLOC)
        /// Returns 100 - MI' (higher weight = harder to maintain).
        /// </summary>
        private static double CalculateMI(double cyclo, double halvol, int sloc)
        {
            cyclo = Math.Max(1.0, cyclo);
            halvol = Math.Max(1.0, halvol);
            sloc = Math.Max(1, sloc);

            double mi = 171.0 - (5.2 * Math.Log(halvol)) - (0.23 * cyclo) - (10.2 * Math.Log(sloc));
            double miNormalized = Math.Max(0, (mi * 100) / 171.0);
            double weight = 100.0 - miNormalized;

            return Math.Max(0, Math.Min(100, weight));
        }


        /// <summary>
        /// Counts source lines of code (excluding comments and blank lines).
        /// </summary>
        private static int CountSourceLinesOfCode(MethodDeclarationSyntax method)
        {
            var text = method.GetText().ToString();
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            int slocCount = 0;
            bool inMultilineComment = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip empty lines
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                // Handle multiline comments
                if (inMultilineComment)
                {
                    if (trimmed.Contains("*/"))
                    {
                        inMultilineComment = false;
                    }

                    continue;
                }

                // Check for multiline comment start
                if (trimmed.StartsWith("/*"))
                {
                    if (!trimmed.Contains("*/"))
                    {
                        inMultilineComment = true;
                    }

                    continue;
                }

                // Skip single-line comments
                if (trimmed.StartsWith("//"))
                {
                    continue;
                }

                slocCount++;
            }

            return slocCount;
        }

        /// <summary>
        /// Gets all line numbers covered by a method.
        /// </summary>
        private static List<int> GetMethodLines(MethodDeclarationSyntax method, SyntaxTree tree)
        {
            var methodSpan = tree.GetLineSpan(method.Span);
            int startLine = methodSpan.StartLinePosition.Line + 1;
            int endLine = methodSpan.EndLinePosition.Line + 1;

            var lines = new List<int>();
            for (int i = startLine; i <= endLine; i++)
            {
                lines.Add(i);
            }

            return lines;
        }

        /// <summary>
        /// Finds the method that contains the given line number.
        /// Uses a per-tree cache of precomputed method spans to avoid re-walking
        /// the syntax tree for every line (O(lines × methods) → O(methods) once).
        /// </summary>
        private MethodDeclarationSyntax? FindMethodContainingLine(int lineNumber, SyntaxNode root, SyntaxTree tree)
        {
            IReadOnlyList<MethodSpan> valueFactory(SyntaxTree _) => BuildMethodSpans(root, tree);
            var methodSpans = _methodSpanCache.GetOrAdd(tree, valueFactory);

            foreach (var span in methodSpans)
            {
                if (lineNumber >= span.StartLine && lineNumber <= span.EndLine)
                {
                    return span.Method;
                }
            }

            return null;
        }

        private static IReadOnlyList<MethodSpan> BuildMethodSpans(SyntaxNode root, SyntaxTree tree)
        {
            var spans = new List<MethodSpan>();
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var lineSpan = tree.GetLineSpan(method.Span);
                spans.Add(new MethodSpan(
                    method,
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.EndLinePosition.Line + 1));
            }
            return spans;
        }
        private readonly record struct MethodSpan(MethodDeclarationSyntax Method, int StartLine, int EndLine);
    }
}
