using System;
using Microsoft.Extensions.Logging;

namespace Logging
{
    public static class LoggingExtensionMethods
    {
        public static void LogSuccess(this ILogger logger, string message, params object[] args)
        {
            logger.LogTrace(message, args);
        }

        public static void LogSuccess(this ILogger logger, int eventId, string message, params object[] args)
        {
            logger.LogTrace(eventId, message, args);
        }

        public static void LogSuccess(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.LogTrace(exception, message, args);
        }

        public static void LogSuccess(this ILogger logger, int eventId, Exception exception, string message, params object[] args)
        {
            logger.LogTrace(eventId, exception, message, args);
        }
    }
}
