using System;

namespace Logging
{
    class LoggingRetryExhaustedException : Exception
    {
        public LoggingRetryExhaustedException() : base() { }
        public LoggingRetryExhaustedException(string message) : base(message) { }
        public LoggingRetryExhaustedException(string message, Exception inner) : base(message, inner) { }
    }
}
