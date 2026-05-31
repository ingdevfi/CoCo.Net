using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ComplexityCoverage.Domain.Interfaces;
using System.Collections.Concurrent;

namespace ComplexityCoverage.Domain.Complexity
{
    /// <summary>
    /// Custom syntax tree cache that wraps code in a class if no methods are found.
    /// This is used by McCabeComplexityStrategy to handle test code snippets that don't have containing classes.
    /// </summary>
    internal class WrappingSyntaxTreeCache : ISyntaxTreeCache
    {
        private readonly ConcurrentDictionary<string, object> _cache = new();

        public object GetOrCreateSyntaxTree(string content)
        {
            return _cache.GetOrAdd(content, static key =>
            {
                // Try to parse as-is
                var tree = CSharpSyntaxTree.ParseText(key);
                var root = (CompilationUnitSyntax)tree.GetRoot();
                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

                // If no methods found, wrap in a class
                if (methods.Count == 0)
                {
                    var wrappedCode = $"public class TestClass {{\n{key}\n}}";
                    tree = CSharpSyntaxTree.ParseText(wrappedCode);
                    root = (CompilationUnitSyntax)tree.GetRoot();
                }

                return new CachedSyntaxTreeData(tree, root);
            });
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Calculates cyclomatic complexity at the method level using McCabe's formula.
    /// 
    /// THEORETICAL BACKGROUND:
    /// McCabe's cyclomatic complexity is formally defined as: M = E - N + 2P
    /// where:
    ///   E = number of edges in the control flow graph
    ///   N = number of nodes in the control flow graph
    ///   P = number of connected components (always 1 for a single method)
    /// 
    /// Therefore for a method: M = E - N + 2
    /// 
    /// Equivalent practical formula: M = 1 + number of decision points
    /// where decision points are: if, switch cases, loops, ternary operators, logical operators (&&, ||)
    /// 
    /// Classic Definition: Complexity represents the minimum number of independent paths through a method.
    /// Higher complexity indicates more decision points and harder-to-test code.
    /// 
    /// IMPLEMENTATION APPROACH (METHOD-LEVEL):
    /// All lines within a method share the same complexity value calculated for the entire method.
    /// We count all decision points (branches, loops, operators) within the method.
    /// 
    /// ALGORITHM:
    /// 1. Find the method containing the line
    /// 2. Count all decision points within that method
    /// 3. Apply formula: CC = 1 + decision_points
    /// 4. Cache result per method
    /// 5. Return cached complexity for all lines in the method
    /// 
    /// ADVANTAGES:
    /// - Uses the proper McCabe formula (E - N + 2P is equivalent to 1 + decision_points)
    /// - Method-level metric (all lines share complexity)
    /// - Each method's complexity reflects its number of independent paths
    /// - Aligns with industry standards (Visual Studio, SonarQube)
    /// </summary>
    public class McCabeComplexityStrategy : AbstractComplexityStrategy
    {
        private readonly ConcurrentDictionary<SyntaxTree, Dictionary<MethodDeclarationSyntax, double>> _treeComplexityCache = new();
        private readonly ConcurrentDictionary<SyntaxTree, IReadOnlyList<MethodSpan>> _methodSpanCache = new();

        public McCabeComplexityStrategy() : base(new WrappingSyntaxTreeCache())
        {
        }

        protected override double CalculateLineWeight(int lineNumber, SyntaxNode root, SyntaxTree tree)
        {
            // Per-tree cache keeps method complexities isolated per file and safe under parallel execution
            var methodComplexityCache = _treeComplexityCache.GetOrAdd(tree, _ => []);

            // Find the method containing this line
            var containingMethod = FindMethodContainingLine(lineNumber, root, tree);

            if (containingMethod == null)
            {
                // Line is not in a method → complexity 1
                return 1.0;
            }

            // Check cache
            if (!methodComplexityCache.TryGetValue(containingMethod, out var methodComplexity))
            {
                // Calculate method complexity using McCabe formula
                methodComplexity = CalculateMcCabeComplexity(containingMethod);
                methodComplexityCache[containingMethod] = methodComplexity;
            }

            return methodComplexity;
        }

        /// <summary>
        /// Calculates McCabe cyclomatic complexity for a method.
        /// Formula: CC = 1 + number of decision points
        /// where decision points = if + loops + ternary + switch-case + logical-operators
        /// </summary>
        private static double CalculateMcCabeComplexity(MethodDeclarationSyntax method)
        {
            int decisionPoints = 0;

            // Use DescendantNodes to get ALL nodes under the method
            foreach (var node in method.DescendantNodes())
            {
                switch (node)
                {
                    // Count if statements
                    case IfStatementSyntax ifStmt:
                        decisionPoints++;
                        decisionPoints += CountLogicalOperators(ifStmt.Condition);
                        break;

                    // Count loops
                    case ForStatementSyntax forStmt:
                        decisionPoints++;
                        if (forStmt.Condition != null)
                        {
                            decisionPoints += CountLogicalOperators(forStmt.Condition);
                        }

                        break;

                    case ForEachStatementSyntax:
                        decisionPoints++;
                        break;

                    case WhileStatementSyntax whileStmt:
                        decisionPoints++;
                        decisionPoints += CountLogicalOperators(whileStmt.Condition);
                        break;

                    // Count ternary operators  
                    case ConditionalExpressionSyntax ternary:
                        decisionPoints++;
                        decisionPoints += CountLogicalOperators(ternary.Condition);
                        break;

                    // Count switch cases
                    case SwitchSectionSyntax:
                        decisionPoints++;
                        break;
                }
            }

            // Apply formula: CC = 1 + decision_points
            // This is equivalent to CC = E - N + 2P where P = 1
            return 1.0 + decisionPoints;
        }

        /// <summary>
        /// Counts the number of logical operators (&& and ||) in an expression.
        /// Each logical operator adds a decision point.
        /// </summary>
        private static new int CountLogicalOperators(ExpressionSyntax? expression)
        {
            if (expression == null)
            {
                return 0;
            }

            var nodes = expression.DescendantNodes().ToList();
            nodes.Add(expression);

            return nodes
                .OfType<BinaryExpressionSyntax>()
                .Count(b => b.Kind() == SyntaxKind.LogicalAndExpression || b.Kind() == SyntaxKind.LogicalOrExpression);
        }

        /// <summary>
        /// Finds the method that contains the given line number.
        /// Uses a per-tree cache of precomputed method spans to avoid re-walking
        /// the syntax tree for every line (O(lines × methods) → O(methods) once).
        /// </summary>
        private MethodDeclarationSyntax? FindMethodContainingLine(int lineNumber, SyntaxNode root, SyntaxTree tree)
        {
            var methodSpans = _methodSpanCache.GetOrAdd(tree, valueFactory: treeKey => BuildMethodSpans(root, treeKey));
            foreach (var span in methodSpans)
            {
                if (lineNumber >= span.StartLine && lineNumber <= span.EndLine)
                {
                    return span.Method;
                }
            }

            return null;
        }

        private static List<MethodSpan> BuildMethodSpans(SyntaxNode root, SyntaxTree tree)
        {
            var spans = new List<MethodSpan>();
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                // Use method.Span (not FullSpan) so leading trivia (XML doc comments) is excluded
                // from the method range — those lines should not inherit the method's complexity.
                var startLine = tree.GetLineSpan(method.Span).StartLinePosition.Line + 1;
                var endLine = tree.GetLineSpan(method.Span).EndLinePosition.Line + 1;
                spans.Add(new MethodSpan(method, startLine, endLine));
            }
            return spans;
        }

        private readonly record struct MethodSpan(MethodDeclarationSyntax Method, int StartLine, int EndLine);
    }
}
