using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Complexity
{
    public class McCabeComplexityStrategy : AbstractComplexityStrategy
    {
        private readonly ConcurrentDictionary<SyntaxTree, ConcurrentDictionary<MethodDeclarationSyntax, double>> _treeComplexityCache = new();
        private readonly ConcurrentDictionary<SyntaxTree, IReadOnlyList<MethodSpan>> _methodSpanCache = new();

        public McCabeComplexityStrategy() : base(new WrappingSyntaxTreeCache())
        {
        }

        protected override double CalculateLineWeight(int lineNumber, SyntaxNode root, SyntaxTree tree)
        {
            // Per-tree cache keeps method complexities isolated per file and safe under parallel execution
            var methodComplexityCache = _treeComplexityCache.GetOrAdd(tree, _ => new ConcurrentDictionary<MethodDeclarationSyntax, double>());

            // Find the method containing this line
            var containingMethod = FindMethodContainingLine(lineNumber, root, tree);

            if (containingMethod == null)
            {
                // Line is not inside a method → no executable complexity
                return 0.0;
            }

            return methodComplexityCache.GetOrAdd(containingMethod, m => CalculateMcCabeComplexity(m));
        }

        public override IReadOnlyList<LineComplexity> CalculateWeights(Models.SourceFile file)
        {
            // For McCabe (per-method strategy), calculate weight and contribution for each line
            var cached = new WrappingSyntaxTreeCache().GetOrCreateSyntaxTree(file.Content);
            var data = (CachedSyntaxTreeData)cached;
            var results = new LineComplexity[file.Lines.Count];

            // Dictionary to map line numbers to their contributions
            var lineContributions = new Dictionary<int, double>();

            // Walk through all decision points and mark their line contributions
            foreach (var method in data.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                foreach (var node in method.DescendantNodes())
                {
                    double nodeContribution = 0;
                    switch (node)
                    {
                        case IfStatementSyntax ifStmt:
                            nodeContribution = 1.0 + CountLogicalOperators(ifStmt.Condition);
                            break;
                        case ForStatementSyntax forStmt:
                            nodeContribution = 1.0;
                            if (forStmt.Condition != null)
                                nodeContribution += CountLogicalOperators(forStmt.Condition);
                            break;
                        case ForEachStatementSyntax:
                            nodeContribution = 1.0;
                            break;
                        case WhileStatementSyntax whileStmt:
                            nodeContribution = 1.0 + CountLogicalOperators(whileStmt.Condition);
                            break;
                        case ConditionalExpressionSyntax ternary:
                            nodeContribution = 1.0 + CountLogicalOperators(ternary.Condition);
                            break;
                        case SwitchSectionSyntax:
                        case SwitchExpressionArmSyntax:
                            nodeContribution = 1.0;
                            break;
                        case BinaryExpressionSyntax binaryExpr:
                            if (binaryExpr.Kind() == SyntaxKind.CoalesceExpression)
                                nodeContribution = 1.0;
                            else if ((binaryExpr.Kind() == SyntaxKind.LogicalAndExpression || binaryExpr.Kind() == SyntaxKind.LogicalOrExpression)
                                && !IsInsideHandledCondition(binaryExpr))
                                nodeContribution = 1.0;
                            break;
                    }

                    if (nodeContribution > 0)
                    {
                        var lineSpan = data.Tree.GetLineSpan(node.Span);
                        var startLine = lineSpan.StartLinePosition.Line + 1;
                        lineContributions[startLine] = nodeContribution;
                    }
                }
            }

            // Build results with weights and contributions
            for (int i = 0; i < file.Lines.Count; i++)
            {
                var weight = CalculateLineWeight(file.Lines[i].LineNumber, data.Root, data.Tree);
                var contribution = lineContributions.TryGetValue(file.Lines[i].LineNumber, out var c) ? c : 0.0;
                results[i] = new LineComplexity(weight, contribution);
            }

            return results;
        }

        /// <summary>
        /// Calculates McCabe cyclomatic complexity for a method.
        /// Formula: CC = 1 + number of decision points
        /// where decision points = if + loops + ternary + switch-case + logical-operators
        /// </summary>
        private static double CalculateMcCabeComplexity(MethodDeclarationSyntax method)
        {
            int decisionPoints = 0;

            foreach (var node in method.DescendantNodes())
            {
                switch (node)
                {
                    // if statements — count the branch + any && / || in the condition
                    case IfStatementSyntax ifStmt:
                        decisionPoints++;
                        decisionPoints += CountLogicalOperators(ifStmt.Condition);
                        break;

                    // for loop — condition is optional
                    case ForStatementSyntax forStmt:
                        decisionPoints++;
                        if (forStmt.Condition != null)
                            decisionPoints += CountLogicalOperators(forStmt.Condition);
                        break;

                    case ForEachStatementSyntax:
                        decisionPoints++;
                        break;

                    case WhileStatementSyntax whileStmt:
                        decisionPoints++;
                        decisionPoints += CountLogicalOperators(whileStmt.Condition);
                        break;

                    // ternary operator
                    case ConditionalExpressionSyntax ternary:
                        decisionPoints++;
                        decisionPoints += CountLogicalOperators(ternary.Condition);
                        break;

                    // switch sections (classic switch)
                    case SwitchSectionSyntax:
                        decisionPoints++;
                        break;

                    // switch expression arms
                    case SwitchExpressionArmSyntax:
                        decisionPoints++;
                        break;

                    // null-coalescing operators (?? and ??=) are decision points
                    case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression }:
                        decisionPoints++;
                        break;

                    case AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceAssignmentExpression }:
                        decisionPoints++;
                        break;

                    // Logical && and || that appear OUTSIDE an if/while/for condition
                    // (e.g. in variable initializers, assignments, argument expressions).
                    // Those inside if/while/for are already counted above via CountLogicalOperators,
                    // so we skip nodes whose immediate ancestor is an already-handled condition.
                    case BinaryExpressionSyntax binaryExpr
                        when binaryExpr.Kind() is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression:
                    {
                        if (!IsInsideHandledCondition(binaryExpr))
                            decisionPoints++;
                        break;
                    }
                }
            }

            return 1.0 + decisionPoints;
        }


        private static bool IsInIfOrElseStatement(SyntaxNode node)
        {
            var parent = node.Parent;
            if (parent is BlockSyntax) // if it is a if/else followed by {}
            {
                parent = parent.Parent;
            }

            return parent is IfStatementSyntax;
        }

        /// <summary>
        /// Returns true when a logical binary expression is already counted as part of
        /// an if / while / for condition processed by CountLogicalOperators above,
        /// to avoid double-counting.
        /// </summary>
        private static bool IsInsideHandledCondition(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                switch (parent)
                {
                    case IfStatementSyntax ifStmt when ifStmt.Condition.Contains(node):
                    case WhileStatementSyntax whileStmt when whileStmt.Condition.Contains(node):
                    case ForStatementSyntax forStmt when forStmt.Condition != null && forStmt.Condition.Contains(node):
                    case ConditionalExpressionSyntax ternary when ternary.Condition.Contains(node):
                        return true;
                    // Stop walking up once we leave the expression context
                    case StatementSyntax:
                        return false;
                }
                parent = parent.Parent;
            }
            return false;
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
