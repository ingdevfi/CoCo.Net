using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace ComplexityCoverage.Infrastructure.Reporting
{
    /// <summary>
    /// Produces per-line syntax-highlighted HTML from C# source using Roslyn tokenization.
    /// No semantic model is required — classification is purely syntactic (token kinds + trivia).
    /// Colours are driven by a <see cref="ThemeDefinition"/> so the output matches the chosen theme.
    /// </summary>
    internal static class CSharpSyntaxHighlighter
    {
        // Control-flow keywords that deserve a distinct colour
        private static readonly HashSet<SyntaxKind> ControlFlowKinds =
        [
            SyntaxKind.IfKeyword, SyntaxKind.ElseKeyword,
            SyntaxKind.ForKeyword, SyntaxKind.ForEachKeyword,
            SyntaxKind.WhileKeyword, SyntaxKind.DoKeyword,
            SyntaxKind.SwitchKeyword, SyntaxKind.CaseKeyword,
            SyntaxKind.DefaultKeyword,
            SyntaxKind.BreakKeyword, SyntaxKind.ContinueKeyword,
            SyntaxKind.ReturnKeyword, SyntaxKind.ThrowKeyword,
            SyntaxKind.TryKeyword, SyntaxKind.CatchKeyword,
            SyntaxKind.FinallyKeyword,
            SyntaxKind.GotoKeyword, SyntaxKind.YieldKeyword,
            SyntaxKind.AwaitKeyword,
        ];

        /// <summary>
        /// Parses <paramref name="sourceCode"/> and returns a dictionary mapping
        /// 1-based line numbers to HTML-encoded, syntax-coloured source lines (no newline).
        /// </summary>
        public static Dictionary<int, string> BuildHighlightedLines(string sourceCode, ThemeDefinition theme)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();

            var segments = new List<(int StartLine, int EndLine, string Html)>();

            foreach (var token in root.DescendantTokens(descendIntoTrivia: false))
            {
                foreach (var trivia in token.LeadingTrivia)
                {
                    segments.Add(TriviaSegment(trivia, tree, theme));
                }

                if (token.Span.Length > 0)
                {
                    segments.Add(TokenSegment(token, tree, theme));
                }

                foreach (var trivia in token.TrailingTrivia)
                {
                    segments.Add(TriviaSegment(trivia, tree, theme));
                }
            }

            var lineBuilders = new Dictionary<int, StringBuilder>();

            foreach (var (startLine, _, html) in segments)
            {
                var parts = html.Split('\n');
                for (int i = 0; i < parts.Length; i++)
                {
                    var lineNo = startLine + i;
                    if (!lineBuilders.TryGetValue(lineNo, out var lb))
                    {
                        lineBuilders[lineNo] = lb = new StringBuilder();
                    }

                    lb.Append(parts[i]);
                }
            }

            return lineBuilders.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static (int StartLine, int EndLine, string Html) TokenSegment(SyntaxToken token, SyntaxTree tree, ThemeDefinition theme)
        {
            var colour = ClassifyToken(token, theme);
            var span = tree.GetLineSpan(token.Span);
            var text = token.Text;
            var html = colour == theme.SyntaxDefault
                ? HtmlEncode(text)
                : SpanPerLine(colour, text);

            return (span.StartLinePosition.Line + 1, span.EndLinePosition.Line + 1, html);
        }

        private static (int StartLine, int EndLine, string Html) TriviaSegment(SyntaxTrivia trivia, SyntaxTree tree, ThemeDefinition theme)
        {
            var span = tree.GetLineSpan(trivia.Span);
            var text = trivia.ToFullString();
            var start = span.StartLinePosition.Line + 1;
            var end   = span.EndLinePosition.Line + 1;

            string html = trivia.Kind() switch
            {
                SyntaxKind.SingleLineCommentTrivia
                    or SyntaxKind.MultiLineCommentTrivia
                    or SyntaxKind.SingleLineDocumentationCommentTrivia
                    or SyntaxKind.MultiLineDocumentationCommentTrivia
                        => SpanPerLine(theme.SyntaxComment, text),

                SyntaxKind.PreprocessingMessageTrivia
                    or SyntaxKind.IfDirectiveTrivia
                    or SyntaxKind.ElseDirectiveTrivia
                    or SyntaxKind.ElifDirectiveTrivia
                    or SyntaxKind.EndIfDirectiveTrivia
                    or SyntaxKind.RegionDirectiveTrivia
                    or SyntaxKind.EndRegionDirectiveTrivia
                    or SyntaxKind.DefineDirectiveTrivia
                    or SyntaxKind.UndefDirectiveTrivia
                    or SyntaxKind.PragmaWarningDirectiveTrivia
                    or SyntaxKind.PragmaChecksumDirectiveTrivia
                        => SpanPerLine(theme.SyntaxPreproc, text),

                _ => HtmlEncode(text)
            };

            return (start, end, html);
        }

        private static string ClassifyToken(SyntaxToken token, ThemeDefinition theme)
        {
            var kind = token.Kind();

            if (ControlFlowKinds.Contains(kind))
            {
                return theme.SyntaxControlFlow;
            }

            if (SyntaxFacts.IsKeywordKind(kind) || SyntaxFacts.IsReservedKeyword(kind))
            {
                return theme.SyntaxKeyword;
            }

            return kind switch
            {
                SyntaxKind.StringLiteralToken
                    or SyntaxKind.InterpolatedStringStartToken
                    or SyntaxKind.InterpolatedStringEndToken
                    or SyntaxKind.InterpolatedStringTextToken
                    or SyntaxKind.InterpolatedVerbatimStringStartToken
                    or SyntaxKind.Utf8StringLiteralToken
                    or SyntaxKind.CharacterLiteralToken => theme.SyntaxString,

                SyntaxKind.NumericLiteralToken => theme.SyntaxNumber,

                SyntaxKind.IdentifierToken when LooksLikeTypeName(token.Text) => theme.SyntaxType,

                _ => theme.SyntaxDefault,
            };
        }

        private static bool LooksLikeTypeName(string name)
            => name.Length > 1 && char.IsUpper(name[0]);

        /// <summary>
        /// Wraps each individual line in its own closed &lt;span&gt; so that multi-line
        /// trivia (doc comments, block comments) distributes correctly when split on '\n'.
        /// </summary>
        private static string SpanPerLine(string colour, string text)
        {
            var lines = text.Split('\n');
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append('\n');
                }

                var fragment = lines[i];
                if (fragment.Length > 0)
                {
                    sb.Append($"<span style=\"color:{colour}\">{HtmlEncode(fragment)}</span>");
                }
            }
            return sb.ToString();
        }

        private static string HtmlEncode(string text)
            => System.Net.WebUtility.HtmlEncode(text);
    }
}
