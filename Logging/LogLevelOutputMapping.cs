using Microsoft.Extensions.Logging;

namespace Logging
{
    public static class LogLevelOutputMapping
    {
        public static string Get(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "Success";
                case LogLevel.Debug:
                    return "Debug";
                case LogLevel.Information:
                    return "Info";
                case LogLevel.Warning:
                    return "Warning";
                case LogLevel.Error:
                    return "Error";
                case LogLevel.Critical:
                    return "Critical";
                default:
                    return "None";
            }
        }
    }
}
