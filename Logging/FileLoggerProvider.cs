using Microsoft.Extensions.Logging;

namespace Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        public FileLoggerProvider()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName);
        }

        public void Flush()
        {
            FileLogger.Flush();
        }

        public void Dispose()
        {
        }
    }
}
