using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Interfaces
{
    public interface ICoberturaCoverageProvider
    {
        Task<CoverageMap> ParseAsync(string xmlFilePath);
    }
}
