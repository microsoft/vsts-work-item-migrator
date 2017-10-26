using System;
using Microsoft.Extensions.Logging;

namespace Logging
{
    public class FileLogger : ILogger
    {
        private string CategoryName;

        public static BulkLogger bulkLogger = new BulkLogger();

        public FileLogger(string categoryName)
        {
            CategoryName = categoryName;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            int logDestination = eventId.Id;

            if (eventId.Id == LogDestination.All || eventId.Id == LogDestination.File)
            {
                PerformLogging(logLevel, state, exception, formatter, logDestination);
            }
        }

        private void PerformLogging<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter, int logDestination)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            LogItem logItem = new LogItem(logLevel, DateTime.Now, message, exception, logDestination);
            bulkLogger.WriteToQueue(logItem);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            int comparison = logLevel.CompareTo(MigratorLogging.configMinimumLogLevel);
            return comparison >= 0;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public static void Flush()
        {
            bulkLogger.BulkLoggerLog();
        }
    }
}
