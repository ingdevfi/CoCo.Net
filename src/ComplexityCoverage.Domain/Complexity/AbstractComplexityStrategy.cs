using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Complexity
{
    public abstract class AbstractComplexityStrategy : IComplexityStrategy
    {
        private readonly ISyntaxTreeCache _syntaxTreeCache;

        protected AbstractComplexityStrategy()
        {
            _syntaxTreeCache = new SyntaxTreeCache();
        }

        protected AbstractComplexityStrategy(ISyntaxTreeCache syntaxTreeCache)
        {
            _syntaxTreeCache = syntaxTreeCache;
        }

        public double CalculateWeight(LineOfCode line, SourceFile context)
        {
            var cached = _syntaxTreeCache.GetOrCreateSyntaxTree(context.Content);
            var data = (CachedSyntaxTreeData)cached;
            return this.CalculateLineWeight(line.LineNumber, data.Root, data.Tree);
        }

        public IReadOnlyList<double> CalculateWeights(SourceFile file)
        {
            var cached = _syntaxTreeCache.GetOrCreateSyntaxTree(file.Content);
            var data = (CachedSyntaxTreeData)cached;
            var weights = new double[file.Lines.Count];

            for (int i = 0; i < file.Lines.Count; i++)
            {
                weights[i] = this.CalculateLineWeight(file.Lines[i].LineNumber, data.Root, data.Tree);
            }

            return weights;
        }

        protected abstract double CalculateLineWeight(int lineNumber, SyntaxNode root, SyntaxTree tree);

        protected static bool IsLineWithinNodeSpan(int lineNumber, SyntaxNode node, SyntaxTree tree)
        {
            var lineSpan = tree.GetLineSpan(node.FullSpan);
            return lineNumber >= lineSpan.StartLinePosition.Line + 1 && lineNumber <= lineSpan.EndLinePosition.Line + 1;
        }

        protected static int CountLogicalOperators(ExpressionSyntax expression)
        {
            var nodes = expression.DescendantNodes().ToList();
            nodes.Add(expression);
            return nodes
                .OfType<BinaryExpressionSyntax>()
                .Count(b => b.Kind() == SyntaxKind.LogicalAndExpression || b.Kind() == SyntaxKind.LogicalOrExpression);
        }
    }
}
