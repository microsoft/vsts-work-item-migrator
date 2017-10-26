using Microsoft.Extensions.Logging;
using System;

namespace Logging
{
    internal class LogItemsRecorder : ILogger
    {
        private string categoryName;

        public LogItemsRecorder(string categoryName)
        {
            this.categoryName = categoryName;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
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

            LogItem logItem = new LogItem(logLevel, DateTime.Now, message, exception, eventId.Id);
            MigratorLogging.logItems.Enqueue(logItem);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}