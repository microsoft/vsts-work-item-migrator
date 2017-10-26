using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Logging;

namespace Common
{
    public class RetryHelper
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<RetryHelper>();

        //If you want to retry then catch the exception and throw. 
        //If you do not want to retry then do not throw
        public static async Task<T> RetryAsync<T>(Func<Task<T>> function, int retryCount, int secsDelay = 1)
        {
            return await RetryAsync(function, null, retryCount, secsDelay);
        }

        public static async Task<T> RetryAsync<T>(Func<Task<T>> function, Func<Guid, Exception, Task<Exception>> exceptionHandler, int retryCount, int secsDelay = 1)
        {
            Guid requestId = Guid.NewGuid();
            Exception exception = null;
            bool succeeded = true;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    succeeded = true;
                    return await function();
                }
                catch (Exception ex)
                {
                    exception = TranslateException(requestId, ex);

                    if (exceptionHandler != null)
                    {
                        try
                        {
                            exception = await exceptionHandler(requestId, exception);
                        }
                        catch
                        {
                            // continue with the original exception handling process
                        }
                    }

                    if (exception is RetryPermanentException)
                    {
                        //exit the for loop as we are not retrying for anything considered permanent
                        break;
                    }

                    succeeded = false;
                    Logger.LogTrace(LogDestination.File, $"Sleeping for {secsDelay} seconds and retrying {requestId} again.");

                    await Task.Delay(secsDelay * 1000);
                    
                    // add 1 second to delay so that each delay is slightly incrementing in wait time
                    secsDelay += 1;
                }
                finally
                {
                    if (succeeded && i >= 1)
                    {
                        Logger.LogSuccess(LogDestination.File, $"request {requestId} succeeded.");
                    }
                }
            }

            if (exception is null)
            {
                throw new RetryExhaustedException($"Retry count exhausted for {requestId}.");
            }
            else
            {
                throw exception;
            }
        }

        /// <summary>
        /// Translates the exception to a permanent exception if not retryable
        /// </summary>
        private static Exception TranslateException(Guid requestId, Exception e)
        {
            var ex = UnwrapIfAggregateException(e);
            if (ex is VssServiceException)
            {
                //Retry in following cases only
                //VS402335: QueryTimeoutException
                //VS402490: QueryTooManyConcurrentUsers
                //VS402491: QueryServerBusy
                //TF400733: The request has been canceled: Request was blocked due to exceeding usage of resource 'WorkItemTrackingResource' in namespace 'User.'
                if (ex.Message.Contains("VS402335")
                    || ex.Message.Contains("VS402490")
                    || ex.Message.Contains("VS402491")
                    || ex.Message.Contains("TF400733"))
                {
                    Logger.LogWarning(LogDestination.File, ex, $"VssServiceException exception caught for {requestId}:");
                }
                else
                {
                    //Specific TF or VS errors. No need to retry DO NOT THROW
                    return new RetryPermanentException($"Permanent error for {requestId}, not retrying", ex);
                }
            }
            else if (ex is HttpRequestException)
            {
                // all request exceptions should be considered retryable
                Logger.LogWarning(LogDestination.File, ex, $"HttpRequestException exception caught for {requestId}:");
            }
            // TF237082: The file you are trying to upload exceeds the supported file upload size
            else if (ex.Message.Contains("TF237082"))
            {
                return new RetryPermanentException($"Permanent error for {requestId}, not retrying", ex);
            }
            else
            {
                //Log and throw every other exception for now - example HttpServiceException for connection errors
                //Need to retry - in case of connection timeouts or server unreachable etc.
                Logger.LogWarning(LogDestination.File, ex, $"Exception caught for {requestId}:");
            }

            return ex;
        }

        private static Exception UnwrapIfAggregateException(Exception e)
        {
            Exception ex;
            //Async calls returns AggregateException
            //Sync calls returns exception
            if (e is AggregateException)
            {
                ex = e.InnerException;
            }
            else
            {
                ex = e;
            }

            return ex;
        }
    }
}
