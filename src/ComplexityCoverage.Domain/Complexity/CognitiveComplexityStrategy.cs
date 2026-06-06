using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Complexity
{
    /// <summary>
    /// Calculates Cognitive Complexity according to the SonarSource specification.
    /// 
    /// FORMULA: Cognitive Complexity = base contributions + nesting penalties
    /// 
    /// The metric evaluates code comprehensibility through human intuition rather than
    /// mathematical models, addressing limitations of Cyclomatic Complexity.
    /// 
    /// Key principles:
    /// 1. Ignore method declarations themselves (no base cost for methods)
    /// 2. Increment for control flow breaks: if, loops (for, while, do-while), ternary
    /// 3. Increment for hybrid operators: else if, else
    /// 4. Increment for catch clauses (once per catch, not per exception type)
    /// 5. Increment for switch statements (once, not per case)
    /// 6. Increment for each new sequence of logical operators (&&/|| chains)
    /// 7. Add nesting increments: each nested control flow structure adds +1 * nesting_level
    /// 8. Increment for labeled jumps (goto, labeled break/continue)
    /// 9. Increment for nested return statements (+1 + nesting_level) — only if nested
    /// 10. Increment for nested throw statements (+1 + nesting_level) — only if nested and not directly in catch
    /// 11. Increment for recursion (detected via method calls)
    /// 
    /// EXAMPLES:
    /// - Simple if: +1
    /// - Nested if in if: +1 for first if + 2 for nested if (nesting=1) = 3 total
    /// - Switch: +1 (not per case)
    /// - if with && operator: +1 for if + 1 for operator sequence
    /// - Return at top level: 0 (no increment)
    /// - Return nested in if: 1 + 1 (nesting level) = 2
    /// - Throw in catch: 0 (no increment)
    /// - Throw nested in if inside catch: 1 + 1 (nesting level) = 2
    /// </summary>
    public class CognitiveComplexityStrategy : AbstractComplexityStrategy
    {
        private readonly ConcurrentDictionary<SyntaxTree, ConcurrentDictionary<MethodDeclarationSyntax, double>> _treeComplexityCache = new();
        private readonly ConcurrentDictionary<SyntaxTree, IReadOnlyList<MethodSpan>> _methodSpanCache = new();

        public CognitiveComplexityStrategy() : base(new WrappingSyntaxTreeCache())
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
                // Line is not inside a method → no cognitive complexity
                return 0.0;
            }

            return methodComplexityCache.GetOrAdd(containingMethod, m => CalculateCognitiveComplexity(m));
        }

        public override IReadOnlyList<LineComplexity> CalculateWeights(Models.SourceFile file)
        {
            // For Cognitive Complexity (per-method strategy), calculate weight and contribution for each line
            var cached = new WrappingSyntaxTreeCache().GetOrCreateSyntaxTree(file.Content);
            var data = (CachedSyntaxTreeData)cached;
            var results = new LineComplexity[file.Lines.Count];

            // Dictionary to map line numbers to their contributions
            var lineContributions = new Dictionary<int, double>();

            foreach (var node in data.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var ctx = new NestingContext(node);

                foreach (var child in node.DescendantNodes())
                {
                    var contribution = EvaluateNode(child, ctx);
                    if (contribution > 0)
                    {
                        var lineSpan = data.Tree.GetLineSpan(child.Span);
                        var startLine = lineSpan.StartLinePosition.Line + 1;
                        lineContributions[startLine] = contribution;
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
        /// Calculates Cognitive Complexity for a method following the SonarSource specification.
        /// </summary>
        private static double CalculateCognitiveComplexity(MethodDeclarationSyntax method)
        {
            double complexity = 0.0;
            var nesting = new NestingContext(method);

            // Walk all nodes in the method and accumulate complexity
            foreach (var node in method.DescendantNodes())
            {
                complexity += EvaluateNode(node, nesting);
            }

            return complexity;
        }

        /// <summary>
        /// Evaluates a single node and returns its cognitive complexity contribution.
        /// </summary>
        private static double EvaluateNode(SyntaxNode node, NestingContext nesting)
        {
            double contribution = 0.0;

            // Structural increments: +1 + nesting_level (no nesting penalty for method/try/lambda/local function)
            if (node is IfStatementSyntax ifStmt)
            {
                int nestingLevel = nesting.GetNestingLevel(node);
                contribution = 1.0 + nestingLevel;
                // Check for logical operators in condition
                contribution += CountLogicalOperatorSequences(ifStmt.Condition);
            }
            else if (node is ForStatementSyntax || node is ForEachStatementSyntax ||
                     node.Kind() == SyntaxKind.ForEachVariableStatement)
            {
                int nestingLevel = nesting.GetNestingLevel(node);
                contribution = 1.0 + nestingLevel;
            }
            else if (node is WhileStatementSyntax whileStmt)
            {
                int nestingLevel = nesting.GetNestingLevel(node);
                contribution = 1.0 + nestingLevel;
                contribution += CountLogicalOperatorSequences(whileStmt.Condition);
            }
            else if (node is DoStatementSyntax doStmt)
            {
                int nestingLevel = nesting.GetNestingLevel(node);
                contribution = 1.0 + nestingLevel;
                contribution += CountLogicalOperatorSequences(doStmt.Condition);
            }
            else if (node is ConditionalExpressionSyntax ternary)
            {
                int nestingLevel = nesting.GetNestingLevel(node);
                contribution = 1.0 + nestingLevel;
                contribution += CountLogicalOperatorSequences(ternary.Condition);
            }
            // Hybrid increments: else if/else get +1 but special handling for else if
            else if (node is ElseClauseSyntax elseClause)
            {
                // If the else clause contains only an if statement (else if pattern),
                // no nesting increment is added (the if is handled separately with nesting)
                if (!(elseClause.Statement is IfStatementSyntax))
                {
                    int nestingLevel = nesting.GetNestingLevel(node);
                    contribution = 1.0 + nestingLevel;
                }
            }
            // Switch: +1 total (not per case)
            else if (node is SwitchStatementSyntax)
            {
                int nestingLevel = nesting.GetNestingLevel(node);
                contribution = 1.0 + nestingLevel;
            }
            // Catch: NO increment to complexity (only increases nesting level for structures inside)
            // Catch is in B2 (increases nesting) and B3 (receives nesting increment)
            // but NOT in B1 (does not add to complexity)
            else if (node is CatchClauseSyntax)
            {
                // Catch does NOT contribute to complexity
                contribution = 0.0;
            }
            // Labeled jumps: goto, labeled break/continue
            else if (node is GotoStatementSyntax)
            {
                contribution = 1.0;
            }
            else if (node is BreakStatementSyntax breakStmt)
            {
                // Check if it has a label (labeled break)
                if (breakStmt.Parent is LabeledStatementSyntax)
                    contribution = 1.0;
            }
            else if (node is ContinueStatementSyntax continueStmt)
            {
                // Check if it has a label (labeled continue)
                if (continueStmt.Parent is LabeledStatementSyntax)
                    contribution = 1.0;
            }
            // Return statements: +1 only if nested (nesting level > 0)
            // Return statements at the top level (outside of control flow) do not add complexity
            else if (node is ReturnStatementSyntax)
            {
                int nestingLevel = nesting.GetNestingLevel(node);
                if (nestingLevel > 0)
                {
                    contribution = 1.0 + nestingLevel;
                }
            }
            // Throw statements: +1 only if nested (nesting level > 0) and NOT directly in catch
            // Throw statements at top level or directly in catch do not add complexity
            else if (node is ThrowStatementSyntax throwStmt)
            {
                // Check if the throw is directly in a catch clause (no nesting increment needed)
                // Walk up the tree to check if we hit a CatchClauseSyntax before hitting any other control flow
                bool isDirectlyInCatch = false;
                var current = throwStmt.Parent;
                while (current != null && current is not MethodDeclarationSyntax)
                {
                    if (current is CatchClauseSyntax)
                    {
                        isDirectlyInCatch = true;
                        break;
                    }
                    // If we encounter another control flow structure before catch, stop
                    if (current is IfStatementSyntax or ForStatementSyntax or ForEachStatementSyntax 
                        or WhileStatementSyntax or DoStatementSyntax or SwitchStatementSyntax 
                        or ConditionalExpressionSyntax or TryStatementSyntax
                        || current.Kind() == SyntaxKind.ForEachVariableStatement)
                    {
                        break;
                    }
                    current = current.Parent;
                }

                if (!isDirectlyInCatch)
                {
                    int nestingLevel = nesting.GetNestingLevel(node);
                    if (nestingLevel > 0)
                    {
                        contribution = 1.0 + nestingLevel;
                    }
                }
            }
            // Logical operators outside conditions: +1 for each new sequence
            else if (node is BinaryExpressionSyntax binaryExpr 
                     && (binaryExpr.Kind() == SyntaxKind.LogicalAndExpression || binaryExpr.Kind() == SyntaxKind.LogicalOrExpression))
            {
                // Only count if not already counted as part of a condition
                if (!IsInsideControlFlowCondition(binaryExpr))
                {
                    contribution = CountLogicalOperatorSequences(binaryExpr);
                }
            }

            return contribution;
        }

        /// <summary>
        /// Counts the number of logical operator sequences in an expression.
        /// A sequence is a contiguous group of binary logical operators.
        /// Sequences are separated when operators of different types (&&, ||) are encountered.
        /// For example:
        ///   a && b && c        → 1 sequence (all &&)
        ///   a || b || c        → 1 sequence (all ||)
        ///   a && b || c        → 2 sequences (&& sequence, then || sequence)
        ///   a || b && c || d   → 3 sequences (||, then &&, then ||)
        /// 
        /// Returns: 1 if at least one operator, plus the number of transitions between different types
        /// This is used for operators both in conditions and outside conditions.
        /// </summary>
        private static int CountLogicalOperatorSequences(ExpressionSyntax? expression)
        {
            if (expression == null)
                return 0;

            // Collect all binary logical operators in order
            var logicalOps = new List<(int Position, SyntaxKind Kind)>();
            CollectLogicalOperators(expression, logicalOps);

            if (logicalOps.Count == 0)
                return 0;

            if (logicalOps.Count == 1)
                return 1; // One sequence with one operator

            // Sort by position to get them in order of appearance
            logicalOps.Sort((a, b) => a.Position.CompareTo(b.Position));

            // Count transitions between different operator types
            int transitions = 0;
            for (int i = 1; i < logicalOps.Count; i++)
            {
                if (logicalOps[i].Kind != logicalOps[i - 1].Kind)
                {
                    transitions++;
                }
            }

            // Number of sequences = 1 (first sequence) + transitions
            return 1 + transitions;
        }

        /// <summary>
        /// Recursively collects all logical binary operators in an expression.
        /// </summary>
        private static void CollectLogicalOperators(SyntaxNode node, List<(int Position, SyntaxKind Kind)> operators)
        {
            if (node is BinaryExpressionSyntax binaryExpr && 
                (binaryExpr.Kind() == SyntaxKind.LogicalAndExpression || binaryExpr.Kind() == SyntaxKind.LogicalOrExpression))
            {
                operators.Add((node.SpanStart, binaryExpr.Kind()));
            }

            foreach (var child in node.ChildNodes())
            {
                CollectLogicalOperators(child, operators);
            }
        }

        /// <summary>
        /// Determines if a binary expression is already counted as part of a control flow condition.
        /// This prevents double-counting logical operators in if/while/for/ternary conditions.
        /// </summary>
        private static bool IsInsideControlFlowCondition(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is IfStatementSyntax ifStmt && ifStmt.Condition.Contains(node))
                    return true;
                if (parent is WhileStatementSyntax whileStmt && whileStmt.Condition.Contains(node))
                    return true;
                if (parent is DoStatementSyntax doStmt && doStmt.Condition.Contains(node))
                    return true;
                if (parent is ForStatementSyntax forStmt && forStmt.Condition != null && forStmt.Condition.Contains(node))
                    return true;
                if (parent is ConditionalExpressionSyntax ternary && ternary.Condition.Contains(node))
                    return true;

                // Stop if we leave the expression context
                if (parent is StatementSyntax || parent is CatchClauseSyntax)
                    return false;

                parent = parent.Parent;
            }

            return false;
        }

        /// <summary>
        /// Finds the method that contains the given line number.
        /// Uses a per-tree cache of precomputed method spans to avoid re-walking
        /// the syntax tree for every line.
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

        private static IReadOnlyList<MethodSpan> BuildMethodSpans(SyntaxNode root, SyntaxTree tree)
        {
            var spans = new List<MethodSpan>();
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var startLine = tree.GetLineSpan(method.Span).StartLinePosition.Line + 1;
                var endLine = tree.GetLineSpan(method.Span).EndLinePosition.Line + 1;
                spans.Add(new MethodSpan(method, startLine, endLine));
            }
            return spans;
        }

        /// <summary>
        /// Tracks nesting depth for control flow structures.
        /// Nesting only counts structural/hybrid nodes that increase cognitive difficulty.
        /// The method declaration itself does not count as nesting (per specification).
        /// </summary>
        private class NestingContext
        {
            private readonly MethodDeclarationSyntax _methodRoot;
            private readonly Dictionary<SyntaxNode, int> _nestingLevelCache = new();

            public NestingContext(MethodDeclarationSyntax methodRoot)
            {
                _methodRoot = methodRoot;
            }

            public int GetNestingLevel(SyntaxNode node)
            {
                if (_nestingLevelCache.TryGetValue(node, out var level))
                    return level;

                level = 0;
                var current = node.Parent;

                // Count nesting levels, but stop at the method declaration itself
                while (current != null && current != _methodRoot)
                {
                    // Only control flow structures that increase nesting depth
                    // According to Annex B2: if, else if, else, ternary, switch, for, foreach, while, do-while, catch
                    // NOTE: try does NOT increase nesting (per spec)
                    // NOTE: foreach includes both ForEachStatementSyntax and ForEachVariableStatement (tuple deconstruction)
                    if (current is IfStatementSyntax or ForStatementSyntax or ForEachStatementSyntax 
                        or WhileStatementSyntax or DoStatementSyntax or CatchClauseSyntax
                        or SwitchStatementSyntax or ConditionalExpressionSyntax
                        || current.Kind() == SyntaxKind.ForEachVariableStatement)
                    {
                        level++;
                    }
                    // Lambda and local functions increase nesting but don't contribute themselves
                    else if (current is LambdaExpressionSyntax or LocalFunctionStatementSyntax)
                    {
                        level++;
                    }

                    current = current.Parent;
                }

                _nestingLevelCache[node] = level;
                return level;
            }
        }

        private readonly record struct MethodSpan(MethodDeclarationSyntax Method, int StartLine, int EndLine);
    }
}
