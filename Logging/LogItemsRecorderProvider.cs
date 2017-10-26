using Microsoft.Extensions.Logging;

namespace Logging
{
    public class LogItemsRecorderProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new LogItemsRecorder(categoryName);
        }

        public void Dispose()
        {
        }
    }
}
