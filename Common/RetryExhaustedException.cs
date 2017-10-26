using System;

namespace Common
{
    class RetryExhaustedException : Exception
    {
        public RetryExhaustedException() : base() { }
        public RetryExhaustedException(string message) : base(message) { }
        public RetryExhaustedException(string message, Exception inner) : base(message, inner) { }
    }
}
