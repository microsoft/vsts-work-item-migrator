using System;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Common.Validation
{
    public class ValidationException : Exception
    {
        public ValidationException() : base() { }
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, AggregateException innerException) : base(message, UnwrapAggregateException(innerException)) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
        public ValidationException(string account, VssUnauthorizedException innerException) : base($"Unable to validate {account}, the scopes \"Work items (read and write) and Identity (read)\" are required.", innerException) { }
        public ValidationException(string account, VssServiceResponseException innerException) : base($"Unable to connect to {account}, please verify the account name.", innerException) { }

        private static Exception UnwrapAggregateException(Exception exception)
        {
            if (exception is AggregateException)
            {
                return ((AggregateException)exception).InnerException;
            }

            return exception;
        }
    }
}
