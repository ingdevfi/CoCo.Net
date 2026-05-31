namespace ComplexityCoverage.Domain.Interfaces
{
    /// <summary>
    /// Simple logging interface for the application.
    /// </summary>
    public interface ILogger
    {
        void Information(string message);
        void Warning(string message);
        void Error(string message, Exception? exception = null);
        void Debug(string message);
    }
}
