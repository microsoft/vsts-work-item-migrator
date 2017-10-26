using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Logging
{
    public class MigratorLogging
    {
        public static ConcurrentQueue<LogItem> logItems = new ConcurrentQueue<LogItem>();
        public static bool recordLoggingIntoLogItems = true;
        public static LogLevel configMinimumLogLevel;

        private static ILoggerFactory LoggerFactory
        {
            get
            {
                LoggerFactory loggerFactory = new LoggerFactory();

                ILoggerProvider consoleLoggerProvider = new ConsoleLoggerProvider();
                loggerFactory.AddProvider(consoleLoggerProvider);

                ILoggerProvider fileLoggerProvider = new FileLoggerProvider();
                loggerFactory.AddProvider(fileLoggerProvider);

                ILoggerProvider logItemsRecorderProvider = new LogItemsRecorderProvider();
                loggerFactory.AddProvider(logItemsRecorderProvider);

                return loggerFactory;
            }
        }

        public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

        public static void LogSummary()
        {
            LoggerFactory loggerFactory = new LoggerFactory();

            ILoggerProvider consoleLoggerProvider = new ConsoleLoggerProvider();
            loggerFactory.AddProvider(consoleLoggerProvider);

            FileLoggerProvider fileLoggerProvider = new FileLoggerProvider();
            loggerFactory.AddProvider(fileLoggerProvider);

            ILogger summaryLogger = loggerFactory.CreateLogger<Object>();

            int eventId = LogDestination.All;

            summaryLogger.LogInformation(eventId, null, "Summary of errors and warnings:");

            foreach (var logItem in logItems)
            {
                LogSummaryItem(logItem, summaryLogger);
            }

            fileLoggerProvider.Flush();
        }

        public static void LogSummaryItem(LogItem logItem, ILogger summaryLogger)
        {
            string output;

            switch (logItem.LogLevel)
            {
                case LogLevel.Warning:
                    output = logItem.OutputFormat(true, false);
                    summaryLogger.LogWarning(logItem.LogDestination, output);
                    break;
                case LogLevel.Error:
                    output = logItem.OutputFormat(true, false);
                    summaryLogger.LogError(logItem.LogDestination, output);
                    break;
                case LogLevel.Critical:
                    output = logItem.OutputFormat(true, false);
                    summaryLogger.LogCritical(logItem.LogDestination, output);
                    break;
            }
        }

        public static string GetLogSummaryText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Summary of errors and warnings:");

            foreach (LogItem logItem in logItems)
            {
                string summaryItem = GetSummaryItem(logItem);

                if (summaryItem != null)
                {
                    stringBuilder.AppendLine(summaryItem);
                }
            }

            return stringBuilder.ToString();
        }

        private static string GetSummaryItem(LogItem logItem)
        {
            if (logItem.LogLevel == LogLevel.Warning || logItem.LogLevel == LogLevel.Error || logItem.LogLevel == LogLevel.Critical)
            {
                return logItem.OutputFormat(true, true);
            }
            else
            {
                return null;
            }
        }
    }
}
