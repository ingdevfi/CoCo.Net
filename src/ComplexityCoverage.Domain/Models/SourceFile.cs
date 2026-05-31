namespace ComplexityCoverage.Domain.Models
{
    public record SourceFile(
        string FilePath, 
        string FileName, 
        string Content,
        IReadOnlyList<LineOfCode> Lines);
}
