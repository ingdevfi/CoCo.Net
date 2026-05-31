using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Interfaces
{
    /// <summary>
    /// Generic interface for parsing any coverage file format into a CoverageMap.
    /// </summary>
    public interface ICoverageFileParser
    {
        Task<CoverageMap> ParseAsync(string filePath);
    }
}
