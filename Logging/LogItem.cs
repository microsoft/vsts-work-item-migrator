using Microsoft.Extensions.Logging;
using System;

namespace Logging
{
    public class LogItem
    {
        public LogLevel LogLevel { get; }

        private DateTime DateTimeStamp { get; }

        public string Message { get; private set; }

        public Exception Exception { get; }

        public int LogDestination { get; }

        public LogItem(LogLevel logLevel, DateTime dateTimeStamp, string message, int logDestination)
        {
            this.LogLevel = logLevel;
            this.DateTimeStamp = dateTimeStamp;
            this.Message = message;
            this.Exception = null;
            this.LogDestination = logDestination;
        }

        public LogItem(LogLevel logLevel, DateTime dateTimeStamp, string message, Exception exception, int logDestination)
        {
            this.LogLevel = logLevel;
            this.DateTimeStamp = dateTimeStamp;
            this.Message = message;
            this.Exception = exception;
            this.LogDestination = logDestination;
        }

        public string OutputFormat(bool includeExceptionMessage, bool includeLogLevelTimeStamp)
        {
            if (includeExceptionMessage && this.Exception != null && this.Exception.Message != null)
            {
                this.Message = $"{this.Message}. {this.Exception.Message}";
            }
            if (includeLogLevelTimeStamp)
            {
                // HH specifies 24-hour time format
                string timeStamp = DateTimeStampString();
                string logLevelName = LogLevelName();
                return $"[{logLevelName}   @{timeStamp}] {this.Message}";
            }
            else
            {
                return this.Message;
            }
        }

        public virtual string DateTimeStampString()
        {
            return this.DateTimeStamp.ToString("HH.mm.ss.fff");
        }

        public virtual string LogLevelName()
        {
            return LogLevelOutputMapping.Get(this.LogLevel);
        }
    }
}
