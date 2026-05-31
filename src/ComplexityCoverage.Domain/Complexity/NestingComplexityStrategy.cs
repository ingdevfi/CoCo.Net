using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;

namespace ComplexityCoverage.Domain.Complexity
{
    /// <summary>
    /// Calculates nesting complexity, emphasizing code structure depth and readability impact.
    /// 
    /// FORMULA: CC = 1 + nesting_depth + logicalOperatorCount
    /// where:
    ///   - Base complexity: 1 (every line has at least this)
    ///   - nesting_depth: The number of control flow decision ancestors (how "deeply nested" the line is)
    ///   - logicalOperatorCount: +1 for each && or || in conditions
    /// 
    /// RATIONALE:
    /// Nesting complexity focuses on code readability and maintainability rather than pure path counting.
    /// Deeply nested code is harder to understand and debug, even if path count is low.
    /// This metric penalizes nested structures (if within if, loop within if, etc.).
    /// 
    /// EXAMPLES:
    /// - Simple line: CC = 1
    /// - Line in one if: CC = 1 + 1 = 2
    /// - Line in if within if: CC = 1 + 2 = 3
    /// - Line in if with && operator: CC = 1 + 1 + 1 = 3
    /// - Line in nested if with && operator: CC = 1 + 2 + 1 = 4
    /// 
    /// ADVANTAGES:
    /// - Directly correlates with readability concerns
    /// - Penalizes deeply nested code (which is harder to maintain)
    /// - Simple and intuitive formula
    /// 
    /// LIMITATIONS:
    /// - Doesn't account for total paths through function (unlike McCabe)
    /// - Switch statements counted as N branches (may over-penalize)
    /// </summary>
    public class NestingComplexityStrategy : AbstractComplexityStrategy
    {
        private readonly ConcurrentDictionary<SyntaxTree, Dictionary<int, double>> _lineWeightCache = new();

        protected override double CalculateLineWeight(int lineNumber, SyntaxNode root, SyntaxTree tree)
        {
            var lineWeights = _lineWeightCache.GetOrAdd(tree, _ => BuildLineWeightMap(root, tree));
            return lineWeights.TryGetValue(lineNumber, out var weight) ? weight : 0.0;
        }

        /// <summary>
        /// Builds a complete map of line number → nesting weight in a single pass.
        /// Walks all nesting-contributing nodes, determines their line spans, 
        /// and accumulates depth contribution per line.
        /// </summary>
        private static Dictionary<int, double> BuildLineWeightMap(SyntaxNode root, SyntaxTree tree)
        {
            var lineWeights = new Dictionary<int, double>();

            // Seed every line that belongs to a method body with base weight 1.0.
            // Lines outside methods (class body, field declarations, …) stay absent
            // from the map and will return 0 via the fallback in CalculateLineWeight.
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Concat<SyntaxNode>(root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
                .Concat(root.DescendantNodes().OfType<LocalFunctionStatementSyntax>()))
            {
                var methodSpan = tree.GetLineSpan(method.Span);
                int mStart = methodSpan.StartLinePosition.Line + 1;
                int mEnd   = methodSpan.EndLinePosition.Line + 1;
                for (int line = mStart; line <= mEnd; line++)
                    lineWeights.TryAdd(line, 1.0);
            }

            // Walk all nesting-contributing nodes once
            foreach (var node in root.DescendantNodes())
            {
                int contribution = GetNestingWeight(node);
                if (contribution == 0)
                {
                    continue;
                }

                // Get the line span of the node's body/block
                var lineSpan = tree.GetLineSpan(node.Span);
                int startLine = lineSpan.StartLinePosition.Line + 1;
                int endLine = lineSpan.EndLinePosition.Line + 1;

                // Add contribution to all lines within this nesting node
                for (int line = startLine; line <= endLine; line++)
                {
                    if (!lineWeights.TryGetValue(line, out var current))
                    {
                        // Line is outside any method — store nesting contribution only (no base)
                        lineWeights[line] = contribution;
                    }
                    else
                    {
                        lineWeights[line] = current + contribution;
                    }
                }
            }

            return lineWeights;
        }

        /// <summary>
        /// Calculates the nesting complexity contribution of a specific syntax node.
        /// </summary>
        private static int GetNestingWeight(SyntaxNode node)
        {
            if (node is IfStatementSyntax ifStmt)
            {
                return 1 + CountLogicalOperators(ifStmt.Condition);
            }

            if (node is WhileStatementSyntax || node is ForStatementSyntax || node is ForEachStatementSyntax)
            {
                return 1;
            }

            if (node is ConditionalExpressionSyntax)
            {
                return 1;
            }

            if (node is SwitchSectionSyntax)
            {
                var switchStmt = node.Ancestors().OfType<SwitchStatementSyntax>().FirstOrDefault();
                return switchStmt?.Sections.Count ?? 0;
            }

            return 0;
        }
    }
}
