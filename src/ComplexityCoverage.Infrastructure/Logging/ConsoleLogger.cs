using ComplexityCoverage.Domain.Interfaces;

namespace ComplexityCoverage.Infrastructure.Logging
{
    /// <summary>
    /// Simple console-based logger implementation.
    /// </summary>
    public class ConsoleLogger(bool verbose = false) : ILogger
    {
        private readonly bool _verbose = verbose;

        public void Information(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {message}");
            Console.ResetColor();
        }

        public void Error(string message, Exception? exception = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[ERROR] {message}");
            if (exception != null)
            {
                Console.Error.WriteLine($"Exception: {exception.Message}");
                if (_verbose)
                {
                    Console.Error.WriteLine($"StackTrace: {exception.StackTrace}");
                }
            }
            Console.ResetColor();
        }

        public void Debug(string message)
        {
            if (_verbose)
            {
                Console.WriteLine($"[DEBUG] {message}");
            }
        }
    }
}
