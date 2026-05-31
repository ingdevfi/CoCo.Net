using Xunit;
using ComplexityCoverage.Infrastructure.Reporting;

namespace ComplexityCoverage.Infrastructure.Tests;

public class CSharpSyntaxHighlighterTests
{
    private static readonly ThemeDefinition Theme = ThemeDefinition.DarkMonokai;

    [Fact]
    public void BuildHighlightedLines_ShouldReturnOneLinePerSourceLine()
    {
        var source = "public class Foo\n{\n    void Bar() {}\n}";
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(source, Theme);

        Assert.Equal(4, lines.Count);
    }

    [Fact]
    public void BuildHighlightedLines_KeywordShouldBeWrappedInSpan()
    {
        var source = "public class Foo { }";
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(source, Theme);

        Assert.Single(lines);
        Assert.Contains("<span", lines[1]);
        Assert.Contains("public", lines[1]);
    }

    [Fact]
    public void BuildHighlightedLines_StringLiteralShouldBeHighlighted()
    {
        var source = "var s = \"hello world\";";
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(source, Theme);

        Assert.Single(lines);
        Assert.Contains(Theme.SyntaxString, lines[1]);
    }

    [Fact]
    public void BuildHighlightedLines_NumericLiteralShouldBeHighlighted()
    {
        var source = "int x = 42;";
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(source, Theme);

        Assert.Single(lines);
        Assert.Contains(Theme.SyntaxNumber, lines[1]);
    }

    [Fact]
    public void BuildHighlightedLines_SingleLineCommentShouldBeHighlighted()
    {
        var source = "// this is a comment";
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(source, Theme);

        Assert.Single(lines);
        Assert.Contains(Theme.SyntaxComment, lines[1]);
    }

    [Fact]
    public void BuildHighlightedLines_MultiLineCommentShouldColorAllLines()
    {
        var source = "/* line 1\n   line 2\n   line 3 */";
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(source, Theme);

        Assert.Equal(3, lines.Count);
        Assert.All(lines.Values, l => Assert.Contains(Theme.SyntaxComment, l));
    }

    [Fact]
    public void BuildHighlightedLines_ControlFlowKeywordShouldUseControlFlowColor()
    {
        var source = "if (x > 0) return x;";
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(source, Theme);

        Assert.Single(lines);
        Assert.Contains(Theme.SyntaxControlFlow, lines[1]);
    }

    [Fact]
    public void BuildHighlightedLines_PreprocessorDirectiveShouldBeHighlighted()
    {
        var source = "#if DEBUG\nvar x = 1;\n#endif";
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(source, Theme);

        Assert.Equal(3, lines.Count);
        Assert.Contains(Theme.SyntaxPreproc, lines[1]);
    }

    [Fact]
    public void BuildHighlightedLines_EmptySource_ShouldReturnEmpty()
    {
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(string.Empty, Theme);

        Assert.Empty(lines);
    }

    [Fact]
    public void BuildHighlightedLines_HtmlSpecialCharsShouldBeEncoded()
    {
        var source = "if (a < b && b > 0) { }";
        var lines = CSharpSyntaxHighlighter.BuildHighlightedLines(source, Theme);

        Assert.Single(lines);
        Assert.Contains("&lt;", lines[1]);
    }
}

