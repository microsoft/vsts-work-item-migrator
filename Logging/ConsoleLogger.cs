using System;
using Microsoft.Extensions.Logging;

namespace Logging
{
    public class ConsoleLogger : ILogger
    {
        private string CategoryName;

        public ConsoleLogger(string categoryName)
        {
            CategoryName = categoryName;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            int logDestination = eventId.Id;
            
            if (logDestination == LogDestination.All || logDestination == LogDestination.Console)
            {
                string output = PerformLogging(logLevel, state, exception, formatter, logDestination);

                if (output != null)
                {
                    Print(logLevel, output);
                }
            }
        }

        private string PerformLogging<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter, int logDestination)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (!IsEnabled(logLevel))
            {
                return null;
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return null;
            }

            LogItem logItem = new LogItem(logLevel, DateTime.Now, message, exception, logDestination);
            return logItem.OutputFormat(true, true);
        }

        public void Print(LogLevel logLevel, string content)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(content);
                    Console.ResetColor();
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(content);
                    Console.ResetColor();
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(content);
                    Console.ResetColor();
                    break;
                case LogLevel.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(content);
                    Console.ResetColor();
                    break;
                default:
                    Console.WriteLine(content);
                    break;
            }
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
