using System;

namespace Common
{
    class RetryPermanentException : Exception
    {
        public RetryPermanentException() : base() { }
        public RetryPermanentException(string message) : base(message) { }
        public RetryPermanentException(string message, Exception inner) : base(message, inner) { }
    }
}
