using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Concurrent;

namespace ComplexityCoverage.Domain.Complexity
{
    /// <summary>
    /// Calculates Halstead Volume complexity metric.
    ///
    /// THEORETICAL BACKGROUND:
    /// Halstead Volume (HV) measures software complexity by analyzing operators and operands.
    /// Formula: HV = N * log2(n)
    /// where:
    ///   N (Length) = Total count of all operators + total count of all operands
    ///   n (Vocabulary) = Count of unique operators + count of unique operands
    ///
    /// OPERATORS ("actions"):
    /// - Keywords: if, for, while, return, etc.
    /// - Mathematical operators: +, -, *, /, %, etc.
    /// - Punctuators: (, ), {, }, ;, etc.
    /// - Instance creations: new ClassName(...)
    /// - Method invocations: methodName(...)
    /// - Assignment operators: =, +=, -=, etc.
    /// - Comparison operators: ==, !=, <, >, <=, >=
    /// - Logical operators: &&, ||, !
    ///
    /// OPERANDS ("data"):
    /// - Variable/field names
    /// - Literals (numbers, strings, booleans, null)
    /// - Constants
    ///
    /// RATIONALE:
    /// Halstead metrics provide insight into code quality from a different angle than cyclomatic complexity.
    /// Higher volume indicates more information content and typically more complex code to understand.
    /// Useful for effort estimation and quality assessment.
    ///
    /// ADVANTAGES:
    /// - Detects different complexity aspects than path-based metrics
    /// - Correlates well with maintenance effort
    /// - Accounts for vocabulary diversity
    /// - Language-independent concept
    ///
    /// LIMITATIONS:
    /// - Different implementations may classify tokens differently
    /// - Doesn't account for control flow patterns
    /// - White-space and formatting affect results
    /// - May not correlate as well with actual defect rates as cyclomatic complexity
    /// </summary>
    public class HalsteadVolumeComplexityStrategy : AbstractComplexityStrategy
    {
        private readonly ConcurrentDictionary<SyntaxTree, Dictionary<int, double>> _lineComplexityCache = new();

        protected override double CalculateLineWeight(int lineNumber, SyntaxNode root, SyntaxTree tree)
        {
            // Per-tree cache keeps results isolated per file and is safe under parallel execution
            var lineComplexity = _lineComplexityCache.GetOrAdd(tree, _ => BuildLineComplexityMap(root, tree));

            // Look up cached complexity for this line, default to 0 if line has no complexity info
            return lineComplexity.TryGetValue(lineNumber, out var complexity) ? complexity : 0.0;
        }

        /// <summary>
        /// Builds a map of line number to Halstead volume complexity.
        /// </summary>
        private static Dictionary<int, double> BuildLineComplexityMap(SyntaxNode root, SyntaxTree tree)
        {
            var allTokens = GetAllTokens(root);

            var operatorsByLine = new Dictionary<int, HashSet<string>>();
            var operandsByLine = new Dictionary<int, HashSet<string>>();
            var operatorCountByLine = new Dictionary<int, int>();
            var operandCountByLine = new Dictionary<int, int>();

            foreach (var token in allTokens)
            {
                int lineNumber = tree.GetLineSpan(token.Span).StartLinePosition.Line + 1;

                if (IsOperator(token))
                {
                    ClassifyToken(token, lineNumber, operatorsByLine, operatorCountByLine);
                }
                else if (IsOperand(token))
                {
                    ClassifyToken(token, lineNumber, operandsByLine, operandCountByLine);
                }
            }

            var map = new Dictionary<int, double>();
            foreach (var line in operatorCountByLine.Keys.Union(operandCountByLine.Keys))
            {
                map[line] = CalculateHalsteadVolume(line, operatorsByLine, operandsByLine, operatorCountByLine, operandCountByLine);
            }

            return map;
        }

        private static void ClassifyToken(SyntaxToken token, int lineNumber, Dictionary<int, HashSet<string>> uniqueByLine, Dictionary<int, int> countByLine)
        {
            if (!uniqueByLine.TryGetValue(lineNumber, out var set))
            {
                set = [];
                uniqueByLine[lineNumber] = set;
                countByLine[lineNumber] = 0;
            }
            set.Add(token.Text);
            countByLine[lineNumber]++;
        }

        private static double CalculateHalsteadVolume(int line, Dictionary<int, HashSet<string>> operatorsByLine, Dictionary<int, HashSet<string>> operandsByLine, Dictionary<int, int> operatorCountByLine, Dictionary<int, int> operandCountByLine)
        {
            int totalOperators = operatorCountByLine.GetValueOrDefault(line);
            int totalOperands = operandCountByLine.GetValueOrDefault(line);
            int N = totalOperators + totalOperands;

            if (N <= 0)
            {
                return 0;
            }

            int uniqueOperators = operatorsByLine.TryGetValue(line, out var ops) ? ops.Count : 0;
            int uniqueOperands = operandsByLine.TryGetValue(line, out var opds) ? opds.Count : 0;
            int n = uniqueOperators + uniqueOperands;

            return n > 1 ? N * Math.Log2(n) : N;
        }

        /// <summary>
        /// Gets all tokens from the syntax tree by walking all descendants.
        /// </summary>
        private static IEnumerable<SyntaxToken> GetAllTokens(SyntaxNode root)
        {
            return root.DescendantTokens();
        }

        /// <summary>
        /// Determines if a token is an operator.
        /// </summary>
        private static bool IsOperator(SyntaxToken token)
        {
            // Keywords (control flow, declarations, modifiers)
            if (token.IsKeyword())
            {
                return true;
            }

            // Operators and punctuation
            return token.Kind() switch
            {
                // Arithmetic operators
                SyntaxKind.PlusToken or
                SyntaxKind.MinusToken or
                SyntaxKind.AsteriskToken or
                SyntaxKind.SlashToken or
                SyntaxKind.PercentToken or

                // Assignment operators
                SyntaxKind.EqualsToken or
                SyntaxKind.PlusEqualsToken or
                SyntaxKind.MinusEqualsToken or
                SyntaxKind.AsteriskEqualsToken or
                SyntaxKind.SlashEqualsToken or
                SyntaxKind.PercentEqualsToken or
                SyntaxKind.AmpersandEqualsToken or
                SyntaxKind.BarEqualsToken or
                SyntaxKind.CaretEqualsToken or
                SyntaxKind.LessThanLessThanEqualsToken or
                SyntaxKind.GreaterThanGreaterThanEqualsToken or

                // Comparison operators
                SyntaxKind.EqualsEqualsToken or
                SyntaxKind.ExclamationEqualsToken or
                SyntaxKind.LessThanToken or
                SyntaxKind.LessThanEqualsToken or
                SyntaxKind.GreaterThanToken or
                SyntaxKind.GreaterThanEqualsToken or

                // Logical operators
                SyntaxKind.AmpersandAmpersandToken or
                SyntaxKind.BarBarToken or
                SyntaxKind.ExclamationToken or

                // Bitwise operators
                SyntaxKind.AmpersandToken or
                SyntaxKind.BarToken or
                SyntaxKind.CaretToken or
                SyntaxKind.TildeToken or
                SyntaxKind.LessThanLessThanToken or
                SyntaxKind.GreaterThanGreaterThanToken or

                // Punctuation
                SyntaxKind.OpenParenToken or
                SyntaxKind.CloseParenToken or
                SyntaxKind.OpenBraceToken or
                SyntaxKind.CloseBraceToken or
                SyntaxKind.OpenBracketToken or
                SyntaxKind.CloseBracketToken or
                SyntaxKind.SemicolonToken or
                SyntaxKind.CommaToken or
                SyntaxKind.DotToken or
                SyntaxKind.ColonToken or
                SyntaxKind.QuestionToken or

                // Increment/Decrement
                SyntaxKind.PlusPlusToken or
                SyntaxKind.MinusMinusToken or

                // Arrow and lambda
                SyntaxKind.EqualsGreaterThanToken or

                // Null coalescing
                SyntaxKind.QuestionQuestionToken or
                SyntaxKind.QuestionQuestionEqualsToken => true,

                _ => false,
            };
        }

        /// <summary>
        /// Determines if a token is an operand (identifier, literal).
        /// </summary>
        private static bool IsOperand(SyntaxToken token)
        {
            return token.Kind() switch
            {
                // Identifiers (variable names, method names, type names)
                SyntaxKind.IdentifierToken or

                // Literals
                SyntaxKind.NumericLiteralToken or
                SyntaxKind.StringLiteralToken or
                SyntaxKind.CharacterLiteralToken or

                // Boolean literals
                SyntaxKind.TrueKeyword or
                SyntaxKind.FalseKeyword or

                // Null literal
                SyntaxKind.NullKeyword => true,

                _ => false,
            };
        }
    }
}
